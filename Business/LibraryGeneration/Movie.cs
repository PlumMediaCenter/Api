using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class Movie
    {
        public Movie(Manager manager, string moviePath)
        {
            this.Manager = manager != null ? manager : new Manager();
            this.FolderPath = moviePath;
        }
        private Manager Manager;

        /// <summary>
        /// A full path to the movie folder (including trailing slash)
        /// </summary>
        private string FolderPath
        {
            get
            {
                return this._FolderPath;
            }
            set
            {
                if (value.EndsWith(Path.DirectorySeparatorChar) == false)
                {
                    value = value + Path.DirectorySeparatorChar;
                }
                this._FolderPath = value;
            }
        }
        private string _FolderPath;

        /// <summary>
        /// 
        /// </summary>
        public async Task Process()
        {
            //if the movie was deleted, remove it from the system
            if (Directory.Exists(this.FolderPath) == false)
            {
                await this.Delete();
            }
            //movie needs updated
            else if (await this.Manager.Movies.Exists(this.FolderPath))
            {
                await this.Update();
            }
            //new movie
            else
            {
                await this.Create();
            }
        }

        public Task Update()
        {
            return Task.CompletedTask;
        }

        public async Task Create()
        {
            var movieDotJson = await this.GetMovieDotJson();
            await this.Manager.Movies.Insert(this.FolderPath, movieDotJson);
        }

        private async Task<MovieDotJson> GetMovieDotJson()
        {
            MovieDotJson result;
            var movieDotJsonPath = $"{this.FolderPath}movie.json";
            if (File.Exists(movieDotJsonPath))
            {
                var contents = await File.ReadAllTextAsync(movieDotJsonPath);
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<MovieDotJson>(contents);
            }
            else
            {
                result = new MovieDotJson();
            }

            //set the title to the folder name if no name was found in the movie.json file
            if (result.Title == null)
            {
                result.Title = new DirectoryInfo(this.FolderPath).Name;
            }
            return result;
        }

        /// <summary>
        /// Get a list of video paths for this video
        /// </summary>
        /// <returns></returns>
        private List<string> PhysicalVideoPaths
        {
            get
            {
                return new List<string>();
            }
        }

        private async Task Delete()
        {
            //delete from the database
            await this.Manager.Movies.Delete(this.FolderPath);
            //delete images from cache
        }
    }
}