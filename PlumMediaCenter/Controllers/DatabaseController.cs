using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Controllers
{
    [ExceptionHandlerFilter()]
    [Route("api/[controller]")]
    public class DatabaseController : Controller
    {
        public DatabaseController(
        )
        {
        }

        [HttpGet("install")]
        public string Install([FromQuery] string username, [FromQuery] string password)
        {
            var installer = new DatabaseInstaller(username, password);
            installer.Install();
            return "Installed database";
        }

        [HttpGet("isInstalled")]
        public async Task<bool> GetIsInstalled()
        {
            return await DatabaseInstaller.IsInstalled();
        }
    }
}