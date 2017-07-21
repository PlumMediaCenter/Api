using System.Collections.Generic;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using System.Linq;

namespace PlumMediaCenter.Business.Managers
{
    public class MovieManager : BaseManager
    {
        public MovieManager(Manager manager = null) : base(manager)
        {

        }

        /// <summary>
        /// Get a list of every movie directory
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetDirectories()
        {
            var rows = await this.Connection.QueryAsync<string>(@"
                select folderPath
                from movies
           ");
            return rows.ToList();
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
        public async Task Insert(string folderPath, MovieDotJson movieDotJson)
        {
            await this.Connection.ExecuteAsync(@"
                insert into movies(folderPath, title, summary, description)
                values(@folderPath, @title, @summary, @description)
            ", new
            {
                folderPath = folderPath,
                title = movieDotJson.Title,
                summary = movieDotJson.Summary,
                description = movieDotJson.Description
            });
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
    }
}