using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class Movie
    {
        public Movie(Manager manager, string moviePath, ulong sourceId)
        {
            this.Manager = manager != null ? manager : new Manager();
            this.FolderPath = moviePath;
            this.SourceId = sourceId;
        }
        private Manager Manager;

        /// <summary>
        /// The id for this video. This is only set during Process(), so don't depend on it unless you are calling a function from Process()
        /// </summary>
        private ulong? Id;

        /// <summary>
        /// The id for the video source
        /// </summary>
        public ulong SourceId;

        /// <summary>
        /// A full path to the movie folder (including trailing slash)
        /// </summary>
        public string FolderPath
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
        /// An MD5 hash of the first chunk of the video file. This helps us detect moved videos
        /// </summary>
        /// <returns></returns>
        public string Md5
        {
            get
            {
                if (_Md5 == null)
                {
                    // //read in the first chunk of the file
                    // var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                }
                return _Md5;
            }
        }
        private string _Md5;

        public string Title
        {
            get
            {
                if (_Title == null)
                {
                    if (this.MovieDotJson != null && string.IsNullOrEmpty(this.MovieDotJson.Title) == false)
                    {
                        _Title = this.MovieDotJson.Title;
                    }
                    else
                    {
                        //use the directory name
                        _Title = new DirectoryInfo(this.FolderPath).Name;
                    }
                }
                return _Title;
            }
        }
        private string _Title;

        public string Summary
        {
            get
            {
                return this.MovieDotJson?.Summary;
            }
        }

        public string Description
        {
            get
            {
                return this.MovieDotJson?.Description;
            }
        }

        public string VideoPath
        {
            get
            {
                if (_VideoPath == null)
                {
                    //find the path to the movie file
                    DirectoryInfo d = new DirectoryInfo(this.FolderPath);
                    foreach (var file in d.GetFiles("*.mp4"))
                    {
                        //keep the first one
                        _VideoPath = $"{this.FolderPath}{file.Name}";
                    }
                }
                return _VideoPath;
            }
        }
        private string _VideoPath;

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
            else if (await this.Manager.LibraryGeneration.Movies.Exists(this.FolderPath))
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

        public Task<ulong?> Update()
        {
            return Task.FromResult<ulong?>(0UL);
        }

        public async Task<ulong?> Create()
        {
            return await this.Manager.LibraryGeneration.Movies.Insert(this);
        }

        public MovieDotJson MovieDotJson
        {
            get
            {
                if (_MovieDotJsonWasRetrieved == false)
                {
                    _MovieDotJsonWasRetrieved = true;
                    var movieDotJsonPath = $"{this.FolderPath}movie.json";
                    if (File.Exists(movieDotJsonPath))
                    {
                        var contents = File.ReadAllText(movieDotJsonPath);
                        _MovieDotJson = Newtonsoft.Json.JsonConvert.DeserializeObject<MovieDotJson>(contents);
                    }
                }
                return _MovieDotJson;
            }
        }
        private MovieDotJson _MovieDotJson;
        private bool _MovieDotJsonWasRetrieved = false;

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
            await this.Manager.LibraryGeneration.Movies.Delete(this.FolderPath);
            //delete images from cache
        }

        private async Task CopyPosters()
        {
            //poster
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
                //the video doesn't have a poster. Create a text-based poster
                this.Manager.Utility.CreateTextPoster(this.Title, destinationPosterPath);
            }
            //backdrop
            var sourceBackdropPath = $"{this.FolderPath}backdrop.jpg";
            var destinationBackdropPath = $"{this.Manager.AppSettings.BackdropFolderPath}{this.Id}.jpg";
            //if the video has a poster, copy it
            if (File.Exists(sourceBackdropPath) == true)
            {
                await Task.Run(() =>
                {
                    File.Copy(sourceBackdropPath, destinationBackdropPath, true);
                });
            }
            else
            {
                //the video doesn't have a backdrop. Create a text-based poster
                this.Manager.Utility.CreateTextBackdrop(this.Title, destinationBackdropPath);
            }
        }
    }
}