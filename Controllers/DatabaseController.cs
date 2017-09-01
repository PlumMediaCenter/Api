using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Attributues;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class DatabaseController : BaseController
    {
        [Route("install")]
        [HttpGet]
        public void Install([FromQuery] string rootUsername, [FromQuery] string rootPassword)
        {
            if (rootUsername == null || rootPassword == null)
            {
                throw new Exception("root username and password required");
            }
            var databaseInstaller = new Data.DatabaseInstaller(rootUsername, rootPassword);
            databaseInstaller.Install();
        }
    }
}
