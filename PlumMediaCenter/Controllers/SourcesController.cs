using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class SourcesController : BaseController
    {
        private readonly MiddlewareInjectorOptions MiddlewareInjectorOptions;

        public SourcesController(MiddlewareInjectorOptions middlewareInjectorOptions)
        {
            this.MiddlewareInjectorOptions = middlewareInjectorOptions;
        }

        /// <summary>
        /// Get all of the sources containing all media items
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IEnumerable<Data.Source>> GetAll()
        {
            return await this.Manager.LibraryGeneration.Sources.GetAll();
        }

        [HttpPost()]
        public async Task SetAll([FromBody] IEnumerable<Source> sources)
        {
            await this.Manager.LibraryGeneration.Sources.SetAll(sources, AppSettings.BaseUrlStatic);

            //update the middleware to serve the new set of sources
            MiddlewareInjectorOptions.InjectMiddleware(app =>
            {
                Startup.RegisterSources(app);
            });
        }

        [HttpGet("settings")]
        public object AppSetings()
        {
            return this.Manager.AppSettings;
        }
    }
}
