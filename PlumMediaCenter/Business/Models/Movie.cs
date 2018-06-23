using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Models
{
    public class Movie : IHasId
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public int SourceId { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> PosterUrls
        {
            get
            {
                if (_PosterUrls == null)
                {
                    var urls = new List<string>();
                    for (var i = 0; i < PosterCount; i++)
                    {
                        urls.Add($"{this.PosterFolderUrl}/{i}.jpg");
                    }
                    _PosterUrls = urls;
                }
                return _PosterUrls;
            }
            set
            {
                _PosterUrls = value;
            }
        }
        private IEnumerable<string> _PosterUrls;
        public int PosterCount;
        public static string CalculateFolderUrl(int sourceId, string folderName, string baseUrl)
        {
            return $"{baseUrl}source{sourceId}/{folderName}/";
        }

        /// <summary>
        /// The base url for the application. This needs to be set per movie in some situations because of how movies are processed in other threads.
        /// It falls back to the request-thread's copy of base url
        /// </summary>
        /// <returns></returns>
        public string BaseUrl
        {
            private get
            {
                return _BaseUrl != null ? _BaseUrl : AppSettings.GetBaseUrlStatic();
            }
            set
            {
                this._BaseUrl = value;
            }
        }
        private string _BaseUrl;

        public string CacheUrl
        {
            get
            {
                return $"{AppSettings.GetImageFolderUrlStatic()}/{this.Id}";
            }
        }
        public string PosterFolderUrl
        {
            get
            {
                return $"{CacheUrl}/posters";
            }
        }

        public string BackdropFolderUrl
        {
            get
            {
                return $"{CacheUrl}/backdrops";
            }
        }

        public IEnumerable<string> BackdropUrls
        {
            get
            {
                if (_BackdropUrls == null)
                {
                    var urls = new List<string>();
                    for (var i = 0; i < BackdropCount; i++)
                    {
                        urls.Add($"{this.BackdropFolderUrl}/{i}.jpg");
                    }
                    _BackdropUrls = urls;
                }
                return _BackdropUrls;
            }
            set
            {
                _BackdropUrls = value;
            }
        }
        private IEnumerable<string> _BackdropUrls;

        public int BackdropCount;

        public string VideoUrl
        {
            get
            {
                //get just the filename from the videopath
                var filename = Path.GetFileName(VideoPath);
                return $"{GetFolderUrl()}{filename}";
            }
        }

        /// <summary>
        /// URL pointing to the folder for this movie
        /// </summary>
        /// <returns></returns>
        public string GetFolderUrl()
        {
            return CalculateFolderUrl(this.SourceId, this.FolderName, this.BaseUrl);
        }

        private string _VideoPath;
        public string VideoPath
        {
            set
            {
                this._VideoPath = value;
            }
            private get
            {
                return this._VideoPath;
            }
        }

        private string _FolderPath;
        public string FolderPath
        {
            set
            {
                this._FolderPath = value;
            }
            private get
            {
                return this._FolderPath;
            }
        }

        /// <summary>
        /// Get the folder path for the movie. Made as a method so it won't serialize to json.
        /// </summary>
        /// <returns></returns>
        public string GetFolderPath()
        {
            return this._FolderPath;
        }

        private string FolderName
        {
            get
            {
                var info = new DirectoryInfo(this.VideoPath);
                return info.Parent.Name;
            }
        }

        public MediaType MediaType = MediaType.MOVIE;

        /// <summary>
        /// The MPAA rating of the movie
        /// </summary>
        public string Rating
        {
            get
            {
                return _Rating != null ? _Rating : "Unknown";
            }
            set
            {
                this._Rating = value;
            }
        }
        private string _Rating;

        /// <summary>
        /// The date that the movie was first released
        /// </summary>
        public int? ReleaseYear { get; set; }
        /// <summary>
        /// The runtime of the movie in seconds
        /// </summary>
        public int RuntimeSeconds { get; set; }
        /// <summary>
        /// The TMDB if of the movie.
        /// </summary>
        public int? TmdbId { get; set; }

        /// <summary>
        /// The number of seconds into a video at which time the video is considered to be completed or watched. 
        /// When not explicitly set by a config file, this value will equal a percentage of the RuntimeSeconds value
        /// </summary>
        public int CompletionSeconds
        {
            get
            {
                if (this._CompletionSeconds == null)
                {
                    this._CompletionSeconds = (int)(this.RuntimeSeconds * ((float)AppSettings.CompletionPercentageStatic / 100));
                }
                return this._CompletionSeconds.Value;
            }
        }
        private int? _CompletionSeconds;

        /// <summary>
        /// The number of seconds that the user last watched this movie until. This must be set externally and will not be 
        /// retrieved 
        /// </summary>
        /// <returns></returns>
        public int ProgressSeconds
        {
            get
            {
                if (this._ProgressSeconds == null)
                {
                    throw new Exception("_ProgressSeconds must be explicitly set on this object, but was found to be null");
                }
                else
                {
                    return this._ProgressSeconds.Value;
                }
            }
            set
            {
                this._ProgressSeconds = value;
            }
        }
        private int? _ProgressSeconds;

        /// <summary>
        /// The number of seconds where the user should resume watching the movie (if they want to pick up where they last left off)
        /// COMPUTED -- not present in the database
        /// </summary>
        /// <returns></returns>
        public int ResumeSeconds
        {
            get
            {
                if (this.ProgressSeconds > this.CompletionSeconds)
                {
                    return 0;
                }
                else
                {
                    return this.ProgressSeconds;
                }
            }
        }

        /// <summary>
        /// The percentage of the movie that the current user has watched
        /// </summary>
        /// <returns></returns>
        public int ProgressPercentage
        {
            get
            {
                return this.ProgressSeconds / this.CompletionSeconds;
            }
        }
    }
}