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
            try
            {
                await ConnectionManager.GetConnection("root", "romantic", false).ExecuteAsync(@"drop database pmc");
            }
            catch (Exception)
            {

            }
            try
            {
                var dbCtrl = new DatabaseController();
                dbCtrl.Install("root", "romantic");
            }
            catch (Exception)
            {

            }

            var libCtrl = new LibraryController();
            await libCtrl.Generate();

            var m = new Business.Manager();
            return await m.Movies.GetAll();
        }
    }
}
