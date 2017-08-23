using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using System.Linq;
using System;

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
            var sources = await this.Manager.LibraryGeneration.Sources.GetAll();
            var rows = await this.Connection.QueryAsync<DbDirResult>(@"
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
        private class DbDirResult
        {
            public string FolderPath;
            public ulong SourceId;
        }

        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task Delete(string movieFolderPath)
        {
            var task = Task.FromResult(new List<string>());
            await task;
        }


        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<ulong?> Insert(LibraryGeneration.Movie movie)
        {
            await this.Connection.ExecuteAsync(@"
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
            return await this.Connection.GetLastInsertIdAsync();
        }


        /// <summary>
        /// Determine if a movie with this folder already exists in the database
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<bool> Exists(string folderPath)
        {
            var result = await this.Connection.QueryAsync<int>(@"
                select count(*) 
                from movies
                where folderPath = @folderPath
            ", new { folderPath = folderPath });
            var count = result.ToList().First();
            return count > 0;
        }


        public async Task<List<Data.Movie>> GetAll()
        {
            var movies = await this.Connection.QueryAsync<Data.Movie>(@"
                select * from movies
            ");
            return movies.ToList();
        }

    }
}