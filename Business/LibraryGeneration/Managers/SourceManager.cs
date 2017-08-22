using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using System.Linq;

namespace PlumMediaCenter.Business.LibraryGeneration.Managers
{
    public class SourceManager : BaseManager
    {
        public SourceManager(Manager manager) : base(manager)
        {

        }

        /// <summary>
        /// Get all of the sources that match the given SourceType (ex: get all movie sources)
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public async Task<List<Source>> GetByType(SourceType sourceType)
        {
            var result = await this.Connection.QueryAsync<Source>(@"
                select * 
                from sources
                where sourceType = @sourceType
            ", new
            {
                sourceType = sourceType
            });
            return result.ToList();
        }

        public async Task<List<Source>> GetAll()
        {
            var task = Task.FromResult(new List<Source>());
            return await task;
        }
    }
}