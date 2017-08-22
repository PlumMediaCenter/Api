using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using PlumMediaCenter.Data;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class MoviesController : Controller
    {
        
        [HttpGet]
        public async Task<List<Models.Movie>> GetAll()
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
            var mgr = new Business.Manager();
            var movies = await mgr.Movies.GetAll();
            return movies;
        }

        [HttpGet]
        [Route("search")]
        public async Task<object> SearchMetadata([FromQuery]string text)
        {

            TMDbClient client = new TMDbClient(new AppSettings().TmdbApiString);
            var movie = await client.GetMovieAsync(47964,
                MovieMethods.AlternativeTitles
                | MovieMethods.Credits
                | MovieMethods.Images
                | MovieMethods.Keywords
                // | MovieMethods.Lists
                | MovieMethods.ReleaseDates
                  // | MovieMethods.Reviews
                  // | MovieMethods.Similar
                  //  | MovieMethods.Translations
                  | MovieMethods.Videos
                );
            return movie;
        }
    }
}
