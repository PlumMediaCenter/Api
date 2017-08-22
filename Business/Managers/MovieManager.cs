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

        public async Task<List<Models.Movie>> GetByIds(List<long> ids)
        {
            var models = await this.Connection.QueryAsync<Models.Movie>(@"
                select * from movies
                where id in :ids
            ", new { ids = ids });
            return models.ToList();
        }
    }
}