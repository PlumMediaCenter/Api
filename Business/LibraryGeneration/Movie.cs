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
        /// The id for this video. This is only set during Process(), so don't depend on it unless you are calling a function from Process()
        /// </summary>
        private decimal? Id;

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
                return;
            }
            //movie needs updated
            else if (await this.Manager.Movies.Exists(this.FolderPath))
            {
                this.Id = await this.Update();
            }
            //new movie
            else
            {
                this.Id = await this.Create();
            }
            await this.CopyPosters();
        }

        public Task<decimal?> Update()
        {
            return Task.FromResult<decimal?>(-1);
        }

        public async Task<decimal?> Create()
        {
            var movieDotJson = await this.GetMovieDotJson();
            return await this.Manager.Movies.Insert(this.FolderPath, movieDotJson);
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

        private async Task CopyPosters()
        {
            var sourcePosterPath = $"{this.FolderPath}poster.jpg";
            var destinationPosterPath = $"{this.Manager.AppSettings.PosterFolderPath}{this.Id}.jpg";
            //if the video has a poster, copy it
            if (File.Exists(sourcePosterPath) == true)
            {
                await Task.Run(() =>
                {
                    File.Copy(sourcePosterPath, destinationPosterPath, true);
                });
            }
            else
            {
                var movieDotJson = await this.GetMovieDotJson();
                var title = movieDotJson.Title;
                //the video doesn't have a poster. Create a text-based poster
                this.Manager.Utility.CreateTextPoster(title, 100, 100, destinationPosterPath);
            }
        }
    }
}