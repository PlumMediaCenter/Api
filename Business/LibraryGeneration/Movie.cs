using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public ulong? GetId()
        {
            return this.Id;
        }

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

        public string Rating
        {
            get
            {
                return this.MovieDotJson?.Rating;
            }
        }

        public DateTime? ReleaseDate
        {
            get
            {
                return this.MovieDotJson?.ReleaseDate;
            }
        }

        public int? Runtime
        {
            get
            {
                if (_Runtime == null)
                {
                    var runtimeFromJson = this.MovieDotJson?.Runtime;
                    if (runtimeFromJson != null)
                    {
                        _Runtime = runtimeFromJson;
                    }
                    else
                    {
                        try
                        {
                            //get runtime from video file 
                            var file = TagLib.File.Create(this.VideoPath);
                            _Runtime = (int?)Math.Ceiling(file.Properties.Duration.TotalMinutes);
                        }
                        catch (Exception)
                        {
                            _Runtime = -1;
                        }
                    }
                }
                if (_Runtime == -1)
                {
                    return null;
                }
                else
                {
                    return _Runtime;
                }
            }
        }
        private int? _Runtime;

        public int? TmdbId
        {
            get
            {
                return this.MovieDotJson?.TmdbId;
            }
        }

        public string BackdropFolderPath
        {
            get
            {
                return Utility.NormalizePath($"{this.FolderPath}backdrops/", false);
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
            await this.CopyImages();
        }

        public async Task<ulong> Update()
        {
            return await this.Manager.LibraryGeneration.Movies.Update(this);
        }

        public async Task<ulong> Create()
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

        /// <summary>
        /// Delete the movie and all of its related records
        /// </summary>
        /// <returns></returns>
        public async Task Delete()
        {
            this.Id = await this.Manager.LibraryGeneration.Movies.GetId(this.FolderPath);
            //delete from the database
            await this.Manager.LibraryGeneration.Movies.Delete(this.FolderPath);

            var imagePaths = new List<string>();
            //delete images from cache
            {
                //poster
                imagePaths.Add($"{this.Manager.AppSettings.PosterFolderPath}{this.Id}.jpg");

                //backdrops
                var guids = this.GetBackdropGuidsFromFilesystem();
                foreach (var guid in guids)
                {
                    imagePaths.Add($"{this.Manager.AppSettings.BackdropFolderPath}{guid}.jpg");
                }
                
                //delete them
                foreach (var imagePath in imagePaths)
                {
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }
                }
            }
        }

        private List<string> GetBackdropGuidsFromFilesystem()
        {
            if (Directory.Exists(this.BackdropFolderPath))
            {
                var files = Directory.GetFiles(this.BackdropFolderPath);
                return files.ToList()
                    .Select(x => Path.GetFileNameWithoutExtension(x))
                    .ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        private async Task CopyImages()
        {
            //poster
            var sourcePosterPath = $"{this.FolderPath}poster.jpg";
            var destinationPosterPath = $"{this.Manager.AppSettings.PosterFolderPath}{this.Id}.jpg";
            //if the video has a poster, copy it
            if (File.Exists(sourcePosterPath) == false)
            {
                //the video doesn't have a poster. Create a text-based poster
                this.Manager.Utility.CreateTextPoster(this.Title, sourcePosterPath);
            }

            //copy the poster
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPosterPath));
            File.Copy(sourcePosterPath, destinationPosterPath, true);

            //backdrop
            var sourceBackdropPath = $"{this.FolderPath}backdrop.jpg";
            var guidsFromDb = await this.Manager.LibraryGeneration.Movies.GetBackdropGuids(this.Id.Value);
            var guidsFromFilesystem = this.GetBackdropGuidsFromFilesystem();

            var backdropPaths = new List<string>();
            foreach (var guid in guidsFromFilesystem)
            {
                //throw out any backdrops that are already in the cache
                var backdropPath = $"{this.BackdropFolderPath}{guid}.jpg";
                var destinationPath = $"{this.Manager.AppSettings.BackdropFolderPath}{guid}.jpg";
                if (File.Exists(destinationPath) == false)
                {
                    backdropPaths.Add(backdropPath);
                }
            }

            //if the movie already has at least one backdrop, we don't need to generate the text-based image
            if (guidsFromFilesystem.Count == 0)
            {
                var backdropPath = $"{this.BackdropFolderPath}{Guid.NewGuid()}.jpg";
                //the video doesn't have a backdrop. Create a text-based image
                this.Manager.Utility.CreateTextBackdrop(this.Title, backdropPath);
                backdropPaths.Add(backdropPath);
            }
            //copy all of the not-yet-cached backdrops to the cached backdrops folder
            foreach (var path in backdropPaths)
            {
                var filename = Path.GetFileName(path);
                var destinationPath = $"{this.Manager.AppSettings.BackdropFolderPath}{filename}";
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(path, destinationPath);
            }
            await this.Manager.LibraryGeneration.Movies.SetBackdropGuids(this.Id.Value, guidsFromFilesystem);
        }
    }
}