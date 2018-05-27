using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Business;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class IsAliveController
    {
        public IsAliveController(
            SearchCatalog searchCatalog
        )
        {
            this.SearchCatalog = searchCatalog;
        }
        SearchCatalog SearchCatalog;

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
