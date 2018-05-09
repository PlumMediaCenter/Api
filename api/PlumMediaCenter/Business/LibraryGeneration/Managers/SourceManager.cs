using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using System.Linq;
using PlumMediaCenter.Business.Enums;

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
        public async Task<IEnumerable<Source>> GetByType(MediaTypeId mediaTypeId)
        {
            var result = await this.QueryAsync<Source>(@"
                select * 
                from Sources
                where mediaTypeId = @mediaTypeId
            ", new
            {
                mediaTypeId = (int)mediaTypeId
            });
            return result;
        }

        public async Task<IEnumerable<Source>> GetAll()
        {

            var result = await this.QueryAsync<Source>(@"
                select * from Sources
            ");
            return result;
        }

        public async Task<int?> Insert(Source source)
        {

            using (var connection = GetNewConnection())
            {
                await connection.ExecuteAsync(@"
                    insert into Sources(folderPath, mediaTypeId)
                    values(@folderPath, @mediaTypeId)
                ", new
                {
                    folderPath = source.FolderPath,
                    mediaTypeId = (int)source.MediaTypeId
                });
                return await connection.GetLastInsertIdAsync();
            }
        }


        public async Task<int?> Update(Source source)
        {
            await this.ExecuteAsync(@"
                update Sources
                set 
                    folderPath = @folderPath,
                    mediaTypeId = @mediaTypeId
                where id = @id
            ", new
            {
                folderPath = source.FolderPath,
                mediaTypeId = (int)source.MediaTypeId,
                id = source.Id
            });
            return source.Id;
        }

        public async Task Delete(int id, string baseUrl)
        {
            //delete all of the movies associated with this source
            await this.Manager.LibraryGeneration.Movies.DeleteForSource(id, baseUrl);

            await this.ExecuteAsync(@"
                delete from Sources
                where id = @id
            ", new
            {
                id = id
            });
        }

        public async Task<int?> Save(Source source)
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
        public async Task SetAll(IEnumerable<Source> sources, string baseUrl)
        {
            var existingSources = await this.GetAll();
            var existingIds = existingSources.Select(x => x.Id);
            var incomingIds = sources.Select(x => x.Id);
            var deleteCandidateIds = existingIds.Where(existingId => incomingIds.Contains(existingId) == false);

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