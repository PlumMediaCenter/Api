using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using System.Linq;
using System;
using System.IO;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.Factories;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Metadata;

namespace PlumMediaCenter.Business.Repositories
{
    public class LibGenMovieRepository
    {
        public LibGenMovieRepository(
            SourceRepository sourceRepository,
            MediaItemRepository mediaItemRepository,
            LibGenFactory libGenFactory
        )
        {
            this.SourceRepository = sourceRepository;
            this.MediaItemRepository = mediaItemRepository;
            this.LibGenFactory = libGenFactory;
        }
        SourceRepository SourceRepository;
        MediaItemRepository MediaItemRepository;
        LibGenFactory LibGenFactory;

        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<int, IEnumerable<string>>> GetDirectories()
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                var sources = await this.SourceRepository.GetAll();
                var rows = await connection.QueryAsync<DbDirResult>(@"
                    select folderPath, sourceId 
                    from Movies
                ");

                var results = rows
                    .GroupBy(x => x.SourceId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Select(y => y.FolderPath)
                    );
                return results;
            }
        }
        private class DbDirResult
        {
            public string FolderPath { get; set; }
            public int SourceId { get; set; }
        }

        /// <summary>
        /// Get the id for the movie at the given path, or null if not found
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<int?> GetId(string folderPath)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                var rows = await connection.QueryAsync<int?>(@"
                    select id 
                    from Movies
                    where folderPath = @folderPath",
                new
                {
                    folderPath = folderPath
                });
                return rows.FirstOrDefault();
            }
        }

        /// <summary>
        /// Delete the movie at the specified path
        /// </summary>
        /// <returns></returns>
        public async Task Delete(string folderPath)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                await connection.ExecuteAsync(@"
                    delete from Movies
                    where folderPath = @folderPath
                ", new
                {
                    folderPath = folderPath
                });
            }
        }


        /// <summary>
        /// Create a basic movie record. The rest of the info will be set in the update proc
        /// </summary>
        /// <returns></returns>
        public async Task<int> Insert(string folderPath, string videoPath, int sourceId)
        {
            Console.WriteLine("Movie.Insert -> Movie VideoPath: " + videoPath);
            var mediaItemId = await this.MediaItemRepository.GetNewMediaId(MediaType.MOVIE);
            await ConnectionManager.ExecuteAsync(@"
                insert into Movies(
                    id,
                    folderPath, 
                    videoPath, 
                    sourceId
                )
                values(
                    @id,
                    @folderPath,
                    @videoPath, 
                    @sourceId
                )
            ", new
            {
                id = mediaItemId,
                folderPath = folderPath,
                videoPath = videoPath,
                sourceId = sourceId
            });
            return mediaItemId;
        }

        public async Task Update(DynamicParameters record)
        {
            var sql = $@"
                update movies 
                set {string.Join(",", record.ParameterNames.Select(x => $"{x}=@{x}"))}
                where id = @id";
            await ConnectionManager.ExecuteAsync(sql, record);
        }

        /// <summary>
        /// Determine if a movie with this folder already exists in the database
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<bool> ExistsInDb(string folderPath)
        {
            var result = await ConnectionManager.QueryAsync<int>(@"
                select count(*) 
                from Movies
                where folderPath = @folderPath
            ", new { folderPath = folderPath });
            var count = result.ToList().First();
            return count > 0;
        }

        /// <summary>
        /// Get a list of guids for all of the backdrops for a movie
        /// </summary>
        /// <param name="movieId"></param>
        /// <returns></returns>
        public async Task<List<string>> GetBackdropGuids(int movieId)
        {
            var rows = await ConnectionManager.QueryAsync<string>(@"
                select backdropGuids from Movies
                where id = @movieId
            ", new { movieId = movieId });

            var queryResult = rows.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(queryResult) == false)
            {
                return queryResult.Split(',').ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Set the list of backdrop guids for a movie 
        /// </summary>
        /// <param name="movieId"></param>
        /// <param name="backdropGuids"></param>
        /// <returns></returns>
        public async Task SetBackdropGuids(int movieId, List<string> backdropGuids)
        {
            var value = string.Join(",", backdropGuids);
            await ConnectionManager.ExecuteAsync(@"
                update Movies
                set backdropGuids = @value
                where id = @movieId
            ", new { movieId = movieId, value = value });
        }

        /// <summary>
        /// Process the movie at the given path. 
        /// </summary>
        /// <param name="moviePath"></param>
        /// <returns></returns>
        public async Task Process(string moviePath, MovieMetadata metadata = null)
        {
            moviePath = Utility.NormalizePath(moviePath, false);
            var sources = await this.SourceRepository.GetAll();
            //remove the movie folder name
            var parentPath = Utility.NormalizePath(Path.GetDirectoryName(Path.GetDirectoryName(moviePath)).ToLowerInvariant(), false);
            var source = sources.Where(x => Utility.NormalizePath(x.FolderPath.ToLowerInvariant(), false) == parentPath).FirstOrDefault();
            var movie = this.LibGenFactory.BuildMovie(moviePath, source.Id);
            await movie.ProcessExistingMovie(metadata);
        }

        public async Task DeleteForSource(int sourceId)
        {
            var folderPaths = await ConnectionManager.QueryAsync<string>(@"
                select folderPath
                from Movies
                where sourceId = @sourceId
            ", new
            {
                sourceId = sourceId
            });
            Parallel.ForEach(folderPaths, (folderPath) =>
            {
                // var manager = new Manager(this.BaseUrl);
                var movie = this.LibGenFactory.BuildMovie(folderPath, sourceId);
                movie.Delete().Wait();
            });
        }
    }
}