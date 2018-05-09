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
using PlumMediaCenter.Attributues;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class MoviesController : BaseController
    {
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
        public async Task<IEnumerable<Models.Movie>> GetAll()
        {
            var movies = await this.Manager.Movies.GetMovies();
            return movies;
        }
    }
}
