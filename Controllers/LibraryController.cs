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
        [Route("generate")]
        [HttpGet]
        public async Task Generate()
        {
            var generator = new LibraryGenerator();
            //temporarily delete all movies
            Data.ConnectionManager.GetConnection().Execute("truncate movies");
            await generator.Generate();
        }
    }
}
