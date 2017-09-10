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
    public class AppSettingsController : BaseController
    {
        [HttpGet("")]
        public object GetAppSetings()
        {
            return this.Manager.AppSettings;
        }
    }
}
