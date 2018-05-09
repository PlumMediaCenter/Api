using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class DatabaseController : BaseController
    {

        [HttpPost("install")]
        public void Install([FromBody] Dictionary<string, string> body)
        {
            if (body.ContainsKey("rootUsername") == false || body.ContainsKey("rootPassword") == false)
            {
                throw new Exception("root username and password required");
            }
            var databaseInstaller = new DatabaseInstaller(body["rootUsername"], body["rootPassword"]);
            databaseInstaller.Install();
        }

        [HttpGet("isInstalled")]
        public async Task<bool> IsInstalled()
        {
            return await DatabaseInstaller.IsInstalled();
        }
    }
}
