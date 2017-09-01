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
        public async Task<List<Data.Source>> GetAll()
        {
            return await this.Manager.LibraryGeneration.Sources.GetAll();
        }

        [HttpPost()]
        public async Task SetAll([FromBody] List<Source> sources)
        {
            await this.Manager.LibraryGeneration.Sources.SetAll(sources);

            //update the middleware to serve the new set of sources
            MiddlewareInjectorOptions.InjectMiddleware(app =>
            {
                Startup.RegisterSources(app);
            });
        }
    }
}
