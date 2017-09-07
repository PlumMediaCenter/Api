using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using System.Linq;
using System;
using System.IO;

namespace PlumMediaCenter.Business.LibraryGeneration.Managers
{
    public class MovieManager : BaseManager
    {
        public MovieManager(Manager manager) : base(manager)
        {

        }

        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<ulong, List<string>>> GetDirectories()
        {
            using (var connection = NewConnection())
            {
                var sources = await this.Manager.LibraryGeneration.Sources.GetAll();
                var rows = await connection.QueryAsync<DbDirResult>(@"
                    select folderPath, sourceId 
                    from movies
                ");

                var results = rows
                    .GroupBy(x => x.SourceId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Select(y => y.FolderPath).ToList()
                    );
                return results;
            }
        }
        private class DbDirResult
        {
            public string FolderPath { get; set; }
            public ulong SourceId { get; set; }
        }

        /// <summary>
        /// Get the id for the movie at the given path, or null if not found
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<ulong?> GetId(string folderPath)
        {
            using (var connection = NewConnection())
            {
                var rows = await connection.QueryAsync<ulong?>(@"
                    select id 
                    from movies
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
            using (var connection = NewConnection())
            {
                await connection.ExecuteAsync(@"
                    delete from movies
                    where folderPath = @folderPath
                ", new
                {
                    folderPath = folderPath
                });
            }
        }


        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> Insert(LibraryGeneration.Movie movie)
        {
            using (var connection = NewConnection())
            {
                await connection.ExecuteAsync(@"
                    insert into movies(
                        folderPath, 
                        videoPath, 
                        title, 
                        summary, 
                        description, 
                        rating,
                        releaseDate,
                        runtime,
                        tmdbId,
                        sourceId
                    )
                    values(
                        @folderPath,
                        @videoPath, 
                        @title, 
                        @summary,
                        @description, 
                        @rating,
                        @releaseDate,
                        @runtime,
                        @tmdbId,
                        @sourceId
                    )
                ", new

                {
                    folderPath = movie.FolderPath,
                    videoPath = movie.VideoPath,
                    title = movie.Title,
                    summary = movie.Summary,
                    description = movie.Description,
                    rating = movie.Rating,
                    releaseDate = movie.ReleaseDate,
                    runtime = movie.Runtime,
                    tmdbId = movie.TmdbId,
                    sourceId = movie.SourceId
                });
                var id = await connection.GetLastInsertIdAsync();
                return id.Value;
            }
        }

        public async Task<ulong> Update(LibraryGeneration.Movie movie)
        {
            using (var connection = NewConnection())
            {
                var movieId = await connection.QueryFirstOrDefaultAsync<ulong?>(@"
                    select id from movies where folderPath = @folderPath
                ", new { folderPath = movie.FolderPath });
                if (movieId == null)
                {
                    throw new Exception($"Movie not found in database with path {movie.FolderPath}");
                }
                await connection.ExecuteAsync(@"
                    update movies
                    set
                        folderPath = @folderPath,
                        videoPath = @videoPath, 
                        title = @title, 
                        summary = @summary,
                        description = @description, 
                        rating = @rating,
                        releaseDate = @releaseDate,
                        runtime = @runtime,
                        tmdbId = @tmdbId,
                        sourceId = @sourceId
                    where id = @movieId
                ", new

                {
                    folderPath = movie.FolderPath,
                    videoPath = movie.VideoPath,
                    title = movie.Title,
                    summary = movie.Summary,
                    description = movie.Description,
                    rating = movie.Rating,
                    releaseDate = movie.ReleaseDate,
                    runtime = movie.Runtime,
                    tmdbId = movie.TmdbId,
                    sourceId = movie.SourceId,
                    movieId = movieId
                });
                return movieId.Value;
            }
        }


        /// <summary>
        /// Determine if a movie with this folder already exists in the database
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<bool> Exists(string folderPath)
        {
            using (var connection = NewConnection())
            {
                var result = await connection.QueryAsync<int>(@"
                    select count(*) 
                    from movies
                    where folderPath = @folderPath
                ", new { folderPath = folderPath });
                var count = result.ToList().First();
                return count > 0;
            }
        }


        public async Task<List<Data.Movie>> GetAll()
        {
            using (var connection = NewConnection())
            {
                var movies = await connection.QueryAsync<Data.Movie>(@"
                    select * from movies
                ");
                return movies.ToList();
            }
        }


        /// <summary>
        /// Get a list of guids for all of the backdrops for a movie
        /// </summary>
        /// <param name="movieId"></param>
        /// <returns></returns>
        public async Task<List<string>> GetBackdropGuids(ulong movieId)
        {
            using (var connection = NewConnection())
            {
                var rows = await connection.QueryAsync<string>(@"
                    select backdropGuids from movies
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
        }

        /// <summary>
        /// Set the list of backdrop guids for a movie 
        /// </summary>
        /// <param name="movieId"></param>
        /// <param name="backdropGuids"></param>
        /// <returns></returns>
        public async Task SetBackdropGuids(ulong movieId, List<string> backdropGuids)
        {
            using (var connection = NewConnection())
            {
                var value = string.Join(",", backdropGuids);
                await connection.ExecuteAsync(@"
                    update movies
                    set backdropGuids = @value
                    where id = @movieId
                ", new { movieId = movieId, value = value });
            }
        }

        /// <summary>
        /// Process the movie at the given path. 
        /// </summary>
        /// <param name="moviePath"></param>
        /// <returns></returns>
        public async Task Process(string moviePath)
        {
            moviePath = Utility.NormalizePath(moviePath, false);
            var sources = await this.Manager.LibraryGeneration.Sources.GetAll();
            //remove the movie folder name
            var parentPath = Utility.NormalizePath(Path.GetDirectoryName(Path.GetDirectoryName(moviePath)).ToLowerInvariant(), false);
            var source = sources.Where(x => Utility.NormalizePath(x.FolderPath.ToLowerInvariant(), false) == parentPath).FirstOrDefault();
            var movie = new Movie(this.Manager, moviePath, source.Id.Value);
            await movie.Process();
        }

        public async Task DeleteForSource(ulong sourceId, string baseUrl)
        {
            using (var connection = NewConnection())
            {
                var folderPaths = await connection.QueryAsync<string>(@"
                    select folderPath
                    from movies
                    where sourceId = @sourceId
                ", new
                {
                    sourceId = sourceId
                });
                Parallel.ForEach(folderPaths, (folderPath) =>
                {
                    var manager = new Manager(this.BaseUrl);
                    var movie = new Movie(manager, folderPath, sourceId);
                    movie.Delete().Wait();
                });
            }
        }
    }
}