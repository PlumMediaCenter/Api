using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using PlumMediaCenter.Data;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using PlumMediaCenter.Business;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    public class MoviesController : Controller
    {
        private Manager _Manager;
        public Manager Manager
        {
            get
            {
                if (_Manager == null)
                {
                    _Manager = new Manager();
                }
                return _Manager;
            }
        }

        [HttpGet("{id}")]
        public async Task<Models.Movie> GetById(int id)
        {
            return await this.Manager.Movies.GetById(id);
        }

        /// <summary>
        /// Get a list of all movies
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<Models.Movie>> GetAll()
        {
            // try
            // {
            //     await ConnectionManager.GetConnection("root", "romantic", false).ExecuteAsync(@"drop database pmc");
            // }
            // catch (Exception e)
            // {

            // }
            // try
            // {
            //     var dbCtrl = new DatabaseController();
            //     dbCtrl.Install("root", "romantic");
            // }
            // catch (Exception e)
            // {

            // }

            // var libCtrl = new LibraryController();
            // await libCtrl.Generate();
            var mgr = new Business.Manager();
            var movies = await mgr.Movies.GetAll();
            return movies;
        }

        [HttpGet]
        [Route("search")]
        public async Task<object> SearchMetadata([FromQuery]string text)
        {

            TMDbClient client = new TMDbClient(new AppSettings().TmdbApiString);
            var movie = await client.GetMovieAsync(27576,
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
