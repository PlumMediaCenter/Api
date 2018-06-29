using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using System.Linq;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Data;
using System;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Repositories
{
    public class SourceRepository
    {
        public SourceRepository(
            Lazy<LibGenMovieRepository> lazyLibGenMovieRepository
        )
        {
            this.LazyLibGenMovieRepository = lazyLibGenMovieRepository;
        }
        Lazy<LibGenMovieRepository> LazyLibGenMovieRepository;
        LibGenMovieRepository LibGenMovieRepository
        {
            get
            {
                return this.LazyLibGenMovieRepository.Value;
            }
        }

        /// <summary>
        /// Get all of the sources that match the given SourceType (ex: get all movie sources)
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Source>> GetByType(MediaType mediaType)
        {
            var result = await ConnectionManager.QueryAsync<Source>(@"
                select 
                    id,
                    folderPath,
                    mediaTypeId as mediaType
                from Sources
                where mediaTypeId = @mediaTypeId
            ", new
            {
                mediaTypeId = (int)mediaType
            });
            return result;
        }

        public async Task<IEnumerable<Source>> GetAll()
        {
            var result = await ConnectionManager.QueryAsync<Source>(@"
                select  
                    id,
                    folderPath,
                    mediaTypeId as mediaType
                from Sources
            ");
            return result;
        }

        public async Task<int> Insert(Source source)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                await connection.ExecuteAsync(@"
                    insert into Sources(folderPath, mediaTypeId)
                    values(@folderPath, @mediaTypeId)
                ", new
                {
                    folderPath = source.FolderPath,
                    mediaTypeId = (int)source.MediaType
                });
                source.Id = (await connection.GetLastInsertIdAsync()).Value;
                return source.Id;
            }
        }


        public async Task<int> Update(Source source)
        {
            await ConnectionManager.ExecuteAsync(@"
                update Sources
                set 
                    folderPath = @folderPath,
                    mediaTypeId = @mediaTypeId
                where id = @id
            ", new
            {
                folderPath = source.FolderPath,
                mediaTypeId = (int)source.MediaType,
                id = source.Id
            });
            return source.Id;
        }

        public async Task Delete(int id)
        {
            //delete all of the movies associated with this source
            await this.LibGenMovieRepository.DeleteForSource(id);

            await ConnectionManager.ExecuteAsync(@"
                delete from Sources
                where id = @id
            ", new
            {
                id = id
            });
        }

        public async Task<int> Save(Source source)
        {
            if (source.Id == 0)
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
        public async Task SetAll(IEnumerable<Source> sources)
        {
            var existingSources = await this.GetAll();
            var existingIds = existingSources.Select(x => x.Id);
            var incomingIds = sources.Select(x => x.Id);
            var deleteCandidateIds = existingIds.Where(existingId => incomingIds.Contains(existingId) == false);

            //delete no longer existant items
            foreach (var id in deleteCandidateIds)
            {
                await this.Delete(id);
            }

            foreach (var source in sources)
            {
                await this.Save(source);
            }
        }
    }
}