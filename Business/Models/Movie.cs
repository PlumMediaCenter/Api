using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlumMediaCenter.Business;
namespace PlumMediaCenter.Models
{
    public class Movie
    {
        public ulong Id;
        public string Title;
        public ulong SourceId;
        public string Description;
        public string PosterUrl
        {
            get
            {
                return $"{AppSettings.BaseUrlStatic}posters/{this.Id}.jpg";
            }
        }

        public string _BackdropGuids
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    BackdropUrlList = new List<string>();
                }
                else
                {

                    BackdropUrlList = value.Split(",").Select(backdropGuid =>
                    {
                        return $"{AppSettings.BaseUrlStatic}backdrops/{backdropGuid}.jpg";
                    }).ToList();
                }
            }
        }
        private List<string> BackdropUrlList;
        public List<string> BackdropUrls
        {
            get
            {
                return BackdropUrlList;
            }
        }

        public string VideoUrl
        {
            get
            {
                //get just the filename from the videopath
                var filename = Path.GetFileName(VideoPath);


                return $"{FolderUrl}{filename}";
            }
        }

        /// <summary>
        /// URL pointing to the folder for this movie
        /// </summary>
        /// <returns></returns>
        public string FolderUrl
        {
            get
            {
                return $"{AppSettings.BaseUrlStatic}source{this.SourceId}/{this.FolderName}/";
            }
        }

        public int Duration;

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

        /// <summary>
        /// The MPAA rating of the movie
        /// </summary>
        public string Rating;
        /// <summary>
        /// The date that the movie was first released
        /// </summary>
        public DateTime? ReleaseDate;
        /// <summary>
        /// The runtime of the movie in minutes
        /// </summary>
        public int Runtime;
        /// <summary>
        /// The TMDB if of the movie.
        /// </summary>
        public ulong? TmdbId;

    }
}