using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Repositories
{
    public class LibGenTvShowRepository
    {
        public LibGenTvShowRepository(
            MediaItemRepository MediaItemRepository
        )
        {
            this.MediaItemRepository = MediaItemRepository;
        }
        MediaItemRepository MediaItemRepository;

        /// <summary>
        /// Get a list of every tv show directory
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetDirectories()
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                var paths = await connection.QueryAsync<string>(@"
                    select folderPath
                    from TvShows
                ");
                return paths;
            }
        }


        /// <summary>
        /// Determine if a serie with this folder already exists in the database
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<bool> ExistsInDb(string folderPath)
        {
            var result = await ConnectionManager.QueryAsync<int>(@"
                select count(*) 
                from Series
                where folderPath = @folderPath
            ", new { folderPath = folderPath });
            var count = result.ToList().First();
            return count > 0;
        }

        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<int> Insert(LibGenTvShow tvShow)
        {
            Console.WriteLine("TvShow.Insert -> TvShow folder path: " + tvShow.FolderPath);
            var mediaItemId = await this.MediaItemRepository.GetNewMediaId(MediaType.TV_SHOW);
            await ConnectionManager.ExecuteAsync(@"
                insert into TvShows(
                    id,
                    folderPath, 
                    title, 
                    sortTitle,
                    summary, 
                    description, 
                    rating,
                    releaseYear,
                    runtimeSeconds,
                    tmdbId,
                    sourceId
                )
                values(
                    @id,
                    @folderPath,
                    @title, 
                    @sortTitle,
                    @summary,
                    @description, 
                    @rating,
                    @releaseYear,
                    @runtimeSeconds,
                    @tmdbId,
                    @sourceId
                )
            ", new
            {
                id = mediaItemId,
                folderPath = tvShow.FolderPath,
                title = tvShow.Title,
                sortTitle = tvShow.SortTitle,
                summary = tvShow.Summary,
                description = tvShow.Description,
                rating = tvShow.Rating,
                releaseYear = tvShow.ReleaseYear,
                runtimeSeconds = tvShow.RuntimeSeconds,
                tmdbId = tvShow.TmdbId,
                sourceId = tvShow.SourceId,
            });
            return mediaItemId;
        }

        public async Task<int> Update(LibGenTvShow tvShow)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                var showId = await connection.QueryFirstOrDefaultAsync<int?>(@"
                    select id from TvShows where folderPath = @folderPath
                ", new { folderPath = tvShow.FolderPath });
                if (showId == null)
                {
                    throw new Exception($"Tv show not found in database with path {tvShow.FolderPath}");
                }
                await connection.ExecuteAsync(@"
                    update TvShows
                    set
                        folderPath = @folderPath,
                        title = @title, 
                        sortTitle = @sortTitle,
                        summary = @summary,
                        description = @description, 
                        rating = @rating,
                        releaseYear = @releaseYear,
                        runtimeSeconds = @runtimeSeconds,
                        tmdbId = @tmdbId,
                        sourceId = @sourceId
                    where id = @showId
                ", new

                {
                    folderPath = tvShow.FolderPath,
                    title = tvShow.Title,
                    sortTitle = tvShow.SortTitle,
                    summary = tvShow.Summary,
                    description = tvShow.Description,
                    rating = tvShow.Rating,
                    releaseYear = tvShow.ReleaseYear,
                    runtimeSeconds = tvShow.RuntimeSeconds,
                    tmdbId = tvShow.TmdbId,
                    sourceId = tvShow.SourceId,
                    showId = showId
                });
                return showId.Value;
            }
        }

        /// <summary>
        /// Get the id for the tv show at the given path, or null if not found
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<int?> GetId(string folderPath)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                var rows = await connection.QueryAsync<int?>(@"
                    select id 
                    from TvShows
                    where folderPath = @folderPath",
                new
                {
                    folderPath = folderPath
                });
                return rows.FirstOrDefault();
            }
        }

        /// <summary>
        /// Delete the tv show at the specified path
        /// </summary>
        /// <returns></returns>
        public async Task Delete(string folderPath)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                await connection.ExecuteAsync(@"
                    delete from TvShow
                    where folderPath = @folderPath
                ", new
                {
                    folderPath = folderPath
                });
            }
        }
    }
}