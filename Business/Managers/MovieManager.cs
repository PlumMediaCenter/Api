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

            var models = await this.Connection.QueryAsync<Models.Movie>(@"
                select * from movies;
            ");
            return models.ToList();
        }

        /// <summary>
        /// Get a list of movies by id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<List<Models.Movie>> GetByIds(List<int> ids)
        {
            var models = await this.Connection.QueryAsync<Models.Movie>(@"
                select * from movies
                where id in @ids
            ", new { ids = ids });
            return models.ToList();
        }

        /// <summary>
        /// Get a movie by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Models.Movie> GetById(int id)
        {
            var movies = await this.GetByIds(new List<int> { id });
            return movies.FirstOrDefault();
        }
    }
}