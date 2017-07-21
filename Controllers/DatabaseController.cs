using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class DatabaseController : Controller
    {
        [Route("install")]
        [HttpGet]
        public void Install([FromQuery] string username, [FromQuery] string password)
        {
            if (username == null || password == null)
            {
                throw new Exception("root username and password required");
            }
            var databaseInstaller = new Data.DatabaseInstaller(username, password);
            databaseInstaller.Install();
        }
    }
}
