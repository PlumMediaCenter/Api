using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PlumMediaCenter;
using PlumMediaCenter.Controllers;
using PlumMediaCenter.Data;
using PlumMediaCenter.Middleware;
using Swashbuckle.AspNetCore.Swagger;

namespace PlumMediaCenter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Data.ConnectionManager.SetDbCredentials("pmc", "pmc");
            var args = Environment.GetCommandLineArgs();
            var virtualDirectoryArgument = args.Where(x => x.Contains("--virtualDirectoryName")).FirstOrDefault();
            if (virtualDirectoryArgument != null)
            {
                try
                {
                    var virtualDirectoryName = virtualDirectoryArgument.Split("=")[1];
                    AppSettings.SetVirtualDirectoryName(virtualDirectoryName);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid virtual directory name");
                }
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
                // var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "pmc.xml");
                // c.IncludeXmlComments(filePath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("CorsPolicy");
            //allow default files (index.html) to be served by default
            app.UseDefaultFiles();
            //serve the wwwroot folder (from the root web url)
            app.UseStaticFiles();
            //register middleware to save the current request to thread storage
            app.UseRequestMiddleware();
            //app.UseDeveloperExceptionPage();

            var injectorOptions = app.ApplicationServices.GetService<MiddlewareInjectorOptions>();
            app.UseMiddlewareInjector(injectorOptions);

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

        }

        /// <summary>
        /// Registers all sources as file server endpoints. 
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterSources(IApplicationBuilder builder)
        {
            try
            {
                var manager = new PlumMediaCenter.Business.Manager();
                var sources = manager.LibraryGeneration.Sources.GetAll().Result;
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
    }
}
