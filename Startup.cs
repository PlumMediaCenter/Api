﻿using System;
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();
            app.UseMvc();

            //get these dynamically from the database
            var locations = new Dictionary<string, string>{
                {@"C:\Videos", "/videos"},
            };
            foreach (var kvp in locations)
            {
                app.UseFileServer(new FileServerOptions()
                {
                    FileProvider = new PhysicalFileProvider(
                        kvp.Key
                    ),
                    RequestPath = new PathString(kvp.Value),
                    EnableDirectoryBrowsing = true
                });
            }
        }
    }
}
