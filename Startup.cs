using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlumMediaCenter;
using PlumMediaCenter.Controllers;
using PlumMediaCenter.Data;
using PlumMediaCenter.Middleware;

namespace PlumMediaCenter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Data.ConnectionManager.SetDbCredentials("pmc", "pmc");
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("CorsPolicy");
            //serve the wwwroot folder (from the root web url)
            app.UseStaticFiles();
            //register middleware to save the current request to thread storage
            app.UseRequestMiddleware();
            app.UseDeveloperExceptionPage();

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
            catch (Exception e)
            {
                //the database is probably not installed.
            }
        }
    }
}
