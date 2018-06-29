using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PlumMediaCenter;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Controllers;
using PlumMediaCenter.Data;
using PlumMediaCenter.Graphql;
using TMDbLib.Client;

namespace PlumMediaCenter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var httpContextAccessor = new HttpContextAccessor();
            services.TryAddSingleton<IHttpContextAccessor>(httpContextAccessor);

            AppSettings.HttpContextAccessor = httpContextAccessor;

            var appSettings = Configuration.GetSection("appSettings").Get<AppSettings>();
            //register a singleton AppSettings
            services.AddSingleton(appSettings);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddSingleton<MiddlewareInjectorOptions>();
            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            });


            //business services
            services.AddSingleton<Utility>();

            //create a new TmdbClient
            var tmdbClient = new TMDbClient(appSettings.TmdbApiString);
            //load default config
            tmdbClient.GetConfig();
            //retry a request 10 times.
            tmdbClient.MaxRetryCount = 10;
            services.AddSingleton(tmdbClient);

            //GraphQL types
            services.AddSingleton<PlumMediaCenter.Graphql.AppSchema>();
            services.AddSingleton<RootQueryGraphType>();
            services.AddSingleton<RootMutationGraphType>();
            //Help GraphQL with DI
            services.AddSingleton<FuncDependencyResolver>(s => new FuncDependencyResolver(type => (GraphType)s.GetService(type)));

            //auto-register all repositories
            foreach (var repoType in typeof(UserRepository).Assembly.GetTypes().Where(t => String.Equals(t.Namespace, "PlumMediaCenter.Business.Repositories", StringComparison.Ordinal)).ToArray())
            {
                //exclude base repo
                if (repoType.Name.StartsWith("BaseRepository"))
                {
                    continue;
                }
                services.AddSingleton(repoType);
            }

            AddServices("PlumMediaCenter.Graphql.GraphTypes", services);
            AddServices("PlumMediaCenter.Graphql.Mutations", services);
            AddServices("PlumMediaCenter.Graphql.InputGraphTypes", services);
            AddServices("PlumMediaCenter.Graphql.EnumGraphTypes", services);
            AddServices("PlumMediaCenter.Business.Repositories", services);
            AddServices("PlumMediaCenter.Business.MetadataProcessing", services);
            AddServices("PlumMediaCenter.Business.Factories", services);


            services.AddSingleton<UserAccessor>();
            services.AddSingleton<LibraryGenerator>();
            services.AddSingleton<DatabaseInstaller>();
            services.AddSingleton<SearchCatalog>();

            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();

            services.AddCors(o => o.AddPolicy("AnyOriginPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
            }));
            services.AddMvc(options => options.OutputFormatters.RemoveType<StringOutputFormatter>());

            //support lazy services
            services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));

            //replace the base url of the index page with the one from appSettings
            ReplaceApiUrl(appSettings.ApiUrl);
            ConnectionManager.SetDbConnectionInfo(appSettings.DbUsername, appSettings.DbPassword, appSettings.DbHost, appSettings.DbName);
        }


        public void AddServices(string namespaceName, IServiceCollection services)
        {

            //auto-register all Query GraphTypes
            foreach (var repoType in typeof(UserRepository).Assembly.GetTypes().Where(t => String.Equals(t.Namespace, namespaceName, StringComparison.Ordinal)).ToArray())
            {
                services.AddSingleton(repoType);
            }
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("CorsPolicy");
            //allow default files (index.html) to be served by default
            app.UseDefaultFiles();
            //serve the wwwroot folder (from the root web url)
            app.UseStaticFiles();
            //app.UseDeveloperExceptionPage();

            var injectorOptions = app.ApplicationServices.GetService<MiddlewareInjectorOptions>();
            app.UseMiddlewareInjector(injectorOptions);
            app.UseMvc();

        }

        /// <summary>
        /// Registers all sources as file server endpoints. 
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterSources(IApplicationBuilder builder)
        {
            try
            {
                var sourceRepository = builder.ApplicationServices.GetService<SourceRepository>();

                var sources = sourceRepository.GetAll().Result;
                foreach (var source in sources)
                {
                    builder.UseFileServer(new FileServerOptions()
                    {
                        FileProvider = new PhysicalFileProvider(
                            source.FolderPath
                        ),
                        RequestPath = new PathString($"/source{source.Id}"),
                        EnableDirectoryBrowsing = true
                    });
                }
            }
            catch (Exception)
            {
                //the database is probably not installed.
            }
        }

        public void ReplaceApiUrl(string apiUrl)
        {
            try
            {
                var indexFilePath = $"{Directory.GetCurrentDirectory()}/wwwroot/index.html";
                if (File.Exists(indexFilePath))
                {
                    var fileContents = File.ReadAllText(indexFilePath);
                    var regexp = new Regex("window.apiUrl\\s*=\\s*.*;");
                    fileContents = regexp.Replace(fileContents, $"window.apiUrl = '{apiUrl}';");
                    File.WriteAllText(indexFilePath, fileContents);
                }
            }
            catch (Exception)
            {

            }
        }
    }
    internal class Lazier<T> : Lazy<T> where T : class
    {
        public Lazier(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }
}
