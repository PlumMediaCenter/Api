using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlumMediaCenter.Business.LibraryGeneration;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class LibraryController : Controller
    {
        [Route("generate")]
        [HttpGet]
        public async Task Install()
        {
           var generator = new LibraryGenerator();
           await generator.Generate();
        }
    }
}
