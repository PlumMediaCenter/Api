using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class VideosController : Controller
    {
        [Route("movies")]
        [HttpGet]
        public async Task<List<Movie>> GetAll()
        {
            var m = new Business.Manager();
            return await m.Movies.GetAll();
        }
    }
}
