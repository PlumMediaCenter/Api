using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Business.LibraryGeneration;
using Dapper;
namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class LibraryController : Controller
    {
        /// <summary>
        /// Start the library generation process. You must monitor /status to determine when the process has completed
        /// </summary>
        /// <returns></returns>
        [HttpGet("generate")]
        public Status Generate()
        {
            var generator = LibraryGenerator.Instance;
            var initialStatus = generator.GetStatus();
            //temporarily delete all movies
            Data.ConnectionManager.GetConnection().Execute("truncate movies");
            Task.Run(() =>
            {
                var libGenTask = generator.Generate();
            });
            var startDate = DateTime.UtcNow;
            //spin until we get a new status
            var status = generator.GetStatus();
            while (status == null || status == initialStatus)
            {
                status = generator.GetStatus();
                var time = DateTime.UtcNow - startDate;
                if (time.TotalSeconds > 20)
                {
                    throw new Exception("Generator status hasn't changed within the expected time");
                }
            }
            return status;
        }

        [HttpGet("status")]
        public Status GetStatus()
        {
            var status = LibraryGenerator.Instance.GetStatus();
            return status;
        }
    }
}
