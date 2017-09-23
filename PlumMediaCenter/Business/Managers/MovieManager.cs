using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace PlumMediaCenter.Business.Managers
{
    public class MovieManager : BaseManager
    {
        public MovieManager(Manager manager) : base(manager)
        {
        }

        public async Task<List<Models.Movie>> GetAll()
        {
            var models = await this.QueryAsync<Models.Movie>(@"
                select *, backdropGuids as _backdropGuids from movies
                order by sortTitle asc;
            ");
            return models.ToList();
        }

        /// <summary>
        /// Get a list of movies by id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<List<Models.Movie>> GetByIds(List<ulong> ids)
        {
            var models = await this.QueryAsync<Models.Movie>(@"
                select *, backdropGuids as _backdropGuids from movies
                where id in @ids
            ", new { ids = ids });
            return models.ToList();
        }

        /// <summary>
        /// Get a movie by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Models.Movie> GetById(ulong id)
        {
            var movies = await this.GetByIds(new List<ulong> { id });
            return movies.FirstOrDefault();
        }
    }
}