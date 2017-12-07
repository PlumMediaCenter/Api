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
    public class IsAliveController : BaseController
    {
        /// <summary>
        /// This is a quick way to find out if the server exists. If this method gets called and returns true, 
        /// the url the client called is correct.
        /// </summary> 
        /// <returns></returns>
        [HttpGet("")]
        public bool GetIsAlive()
        {
            return true;
        }
    }
}
