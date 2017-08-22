using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class SourcesController : Controller
    {

        private readonly MiddlewareInjectorOptions middlewareInjectorOptions;

        public SourcesController(MiddlewareInjectorOptions middlewareInjectorOptions)
        {
            this.middlewareInjectorOptions = middlewareInjectorOptions;
        }

        [HttpGet("reregister-sources")]
        public ActionResult ReRegisterSources()
        {
            middlewareInjectorOptions.InjectMiddleware(app =>
            {
                Startup.RegisterSources(app);
            });
            return Content("File server enabled");
        }
    }
}
