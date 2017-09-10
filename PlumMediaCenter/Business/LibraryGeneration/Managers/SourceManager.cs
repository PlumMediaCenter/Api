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
        public async Task<List<Source>> GetByType(string sourceType)
        {


            using (var connection = NewConnection())
            {
                var result = await connection.QueryAsync<Source>(@"
                    select * 
                    from sources
                    where sourceType = @sourceType
                ", new
                {
                    sourceType = sourceType
                });
                return result.ToList();
            }
        }

        public async Task<List<Source>> GetAll()
        {
            using (var connection = NewConnection())
            {
                var result = await connection.QueryAsync<Source>(@"
                    select * from sources
                ");
                return result.ToList();
            }
        }

        public async Task<ulong?> Insert(Source source)
        {

            using (var connection = NewConnection())
            {
                await connection.ExecuteAsync(@"
                    insert into sources(folderPath, sourceType)
                    values(@folderPath, @sourceType)
                ", new
                {
                    folderPath = source.FolderPath,
                    sourceType = source.SourceType
                });
                return await connection.GetLastInsertIdAsync();
            }
        }


        public async Task<ulong?> Update(Source source)
        {
            using (var connection = NewConnection())
            {

                await connection.ExecuteAsync(@"
                    update sources
                    set 
                        folderPath = @folderPath,
                        sourceType = @sourceType
                    where id = @id
                ", new
                {
                    folderPath = source.FolderPath,
                    sourceType = source.SourceType,
                    id = source.Id
                });
                return source.Id;
            }
        }

        public async Task Delete(ulong id, string baseUrl)
        {
            using (var connection = NewConnection())
            {
                //delete all of the movies associated with this source
                await this.Manager.LibraryGeneration.Movies.DeleteForSource(id, baseUrl);

                await connection.ExecuteAsync(@"
                    delete from sources
                    where id = @id
                ", new
                {
                    id = id
                });
            }
        }

        public async Task<ulong?> Save(Source source)
        {
            if (source.Id == null)
            {
                return await this.Insert(source);
            }
            else
            {
                return await this.Update(source);
            }
        }

        /// <summary>
        /// Save or update all of the provided sources. If an id is present, that is used to update the source
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public async Task SetAll(List<Source> sources, string baseUrl)
        {
            var existingSources = await this.GetAll();
            var existingIds = existingSources.Select(x => x.Id).ToList();
            var incomingIds = sources.Select(x => x.Id).ToList();
            var deleteCandidateIds = existingIds.Where(existingId => incomingIds.Contains(existingId) == false).ToList();

            //delete no longer existant items
            foreach (var id in deleteCandidateIds)
            {
                await this.Delete(id.Value, baseUrl);
            }

            foreach (var source in sources)
            {
                await this.Save(source);
            }
        }
    }
}