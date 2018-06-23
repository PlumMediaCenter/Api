using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Business.Metadata;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Data;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Models
{
    public class LibGenMovie : IProcessable
    {
        public LibGenMovie(
            string moviePath,
            int sourceId,
            LibGenMovieRepository LibGenMovieRepository,
            MovieMetadataProcessor MovieMetadataProcessor,
            AppSettings AppSettings,
            Utility Utility
         )
        {
            this.FolderPath = moviePath;
            this.SourceId = sourceId;

            this.LibGenMovieRepository = LibGenMovieRepository;
            this.MovieMetadataProcessor = MovieMetadataProcessor;
            this.AppSettings = AppSettings;
            this.Utility = Utility;
        }
        LibGenMovieRepository LibGenMovieRepository;
        MovieMetadataProcessor MovieMetadataProcessor;
        AppSettings AppSettings;
        Utility Utility;

        /// <summary>
        /// The id for this video. This is only set during Process(), so don't depend on it unless you are calling a function from Process()
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The source id id for the video's source
        /// </summary>
        public int SourceId { get; set; }

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

        public string Title
        {
            get
            {
                if (_Title == null)
                {
                    var year = this.Utility.GetYearFromFolderName(this.FolderName);
                    if (year != null)
                    {
                        var idx = this.FolderName.LastIndexOf($"({year})");
                        if (idx > -1)
                        {
                            _Title = this.FolderName.Substring(0, idx).Trim();
                        }
                    }
                    else
                    {
                        _Title = this.FolderName;
                    }
                }
                return _Title;
            }
        }
        private string _Title;

        public string SortTitle
        {
            get
            {
                var title = this.Title;
                //remove the word "the" so it sorts better
                if (title.ToLower().StartsWith("the "))
                {
                    return title.Substring(3);
                }
                else
                {
                    return title;
                }
            }
        }

        /// <summary>
        /// Determine the runtime of the video, in seconds, from the MP4 metadata
        /// </summary>
        /// <returns></returns>
        public int? GetRuntimeSeconds()
        {
            if (_RuntimeSeconds == null)
            {
                try
                {
                    //get runtime from video file 
                    var file = TagLib.File.Create(this.VideoPath);
                    _RuntimeSeconds = (int?)Math.Ceiling(file.Properties.Duration.TotalSeconds);
                }
                catch (System.Exception)
                {
                    _RuntimeSeconds = -1;
                }

            }
            if (_RuntimeSeconds == -1)
            {
                return null;
            }
            else
            {
                return _RuntimeSeconds;
            }
        }
        private int? _RuntimeSeconds;

        private string FolderName
        {
            get
            {
                var folderName = new DirectoryInfo(this.FolderPath).Name;
                return folderName;
            }
        }

        private int? GetYearFromFolderName()
        {
            return this.Utility.GetYearFromFolderName(this.FolderName);
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task Process()
        {
            Console.WriteLine($"Process movie: {this.FolderPath}");
            //if the movie was deleted from the filesystem, remove it from the database
            if (Directory.Exists(this.FolderPath) == false)
            {
                Console.WriteLine($"Delete movie from DB: {this.FolderPath}");
                await this.Delete();
                return;
            }

            MovieMetadata metadata = null;
            var record = new DynamicParameters();

            //if this is a new movie
            if (await this.LibGenMovieRepository.ExistsInDb(this.FolderPath) == false)
            {
                Console.WriteLine($"Insert movie into db: {this.FolderPath}");
                this.Id = await this.LibGenMovieRepository.Insert(this.FolderPath, this.VideoPath, this.SourceId);
                metadata = await this.GetMetadataFromTmdb();
                if (metadata != null)
                {
                    record.Add("title", this.Title);
                    record.Add("sortTitle", metadata.SortTitle);
                    record.Add("rating", metadata.Rating);
                    record.Add("releaseYear", metadata.ReleaseYear);
                    record.Add("summary", metadata.Summary);
                    record.Add("tmdbId", metadata.TmdbId);
                }
                else
                {
                    record.Add("title", this.Title);
                    record.Add("sortTitle", this.SortTitle);
                    var year = this.GetYearFromFolderName();
                    if (year != null)
                    {
                        record.Add("year", year);
                    }
                }
                var posterCount = await CopyImages(metadata?.PosterUrls, this.PosterFolderPath, ImageType.Poster);
                var backdropCount = await CopyImages(metadata?.BackdropUrls, this.BackdropFolderPath, ImageType.Backdrop);
                record.Add("posterCount", posterCount);
                record.Add("backdropCount", backdropCount);
            }
            else
            {
                //this is an existing movie. Fetch its id
                await this.LoadId();
            }

            record.Add("id", this.Id);
            record.Add("folderPath", this.FolderPath);
            record.Add("videoPath", this.VideoPath);
            record.Add("runtimeSeconds", this.GetRuntimeSeconds());
            record.Add("sourceId", this.SourceId);


            //update the db with all of the fields we collected
            Console.WriteLine($"Update db record: {this.FolderPath}");
            await this.LibGenMovieRepository.Update(record);
        }

        public async Task<MovieMetadata> GetMetadataFromTmdb()
        {
            var year = this.Utility.GetYearFromFolderName(this.FolderName);
            var title = this.Title;
            //get search results
            var allSearchResults = await this.MovieMetadataProcessor.GetSearchResultsAsync(title);
            var filteredSearchResults = allSearchResults.Where(x => this.Utility.TitlesAreEquivalent(x.Title, title)).ToList();

            //filter the search results by year (if possible)
            if (year != null)
            {
                filteredSearchResults = filteredSearchResults.Where((x) =>
                {
                    return x.ReleaseYear != null &&
                            year != null &&
                            x.ReleaseYear == year.Value;
                }).ToList();
            }
            //if we have any search results left, use the first one
            var firstFilteredSearchResult = filteredSearchResults.FirstOrDefault();
            if (firstFilteredSearchResult == null)
            {
                return null;
            }
            //download TMDB metadata
            var metadata = await this.MovieMetadataProcessor.GetTmdbMetadataAsync(firstFilteredSearchResult.TmdbId);
            return metadata;
        }

        public string CachePath
        {
            get
            {
                return $"{AppSettings.ImageFolderPath}/{this.Id}";
            }
        }

        /// <summary>
        /// The path to the posters folder. Excludes the trailing slash.
        /// </summary>
        /// <returns></returns>
        public string PosterFolderPath
        {
            get
            {
                return $"{this.CachePath}/posters";
            }
        }

        /// <summary>
        /// The path to the backdrops folder. Excludes the trailing slash.
        /// </summary>
        /// <returns></returns>
        public string BackdropFolderPath
        {
            get
            {
                return $"{this.CachePath}/backdrops";
            }
        }

        private async Task LoadId()
        {
            if (this.Id == null)
            {
                this.Id = await this.LibGenMovieRepository.GetId(this.FolderPath);
            }

        }

        /// <summary>
        /// Delete the movie and all of its related records
        /// </summary>
        /// <returns></returns>
        public async Task Delete()
        {
            await this.LoadId();

            //delete from the database
            await this.LibGenMovieRepository.Delete(this.FolderPath);

            var imagePaths = new List<string>();

            //delete images from cache
            Directory.Delete(this.CachePath, true);
        }

        private enum ImageType
        {
            Poster = 1,
            Backdrop = 2
        };

        private async Task<int> CopyImages(List<string> imageUrls, string destinationFolderPath, ImageType imageType)
        {
            imageUrls = imageUrls ?? new List<string>();
            var webClient = new WebClient();
            var imageCount = 0;
            var tempPath = $"{this.CachePath}/tmp/{Guid.NewGuid()}";

            //download all of the posters 
            if (imageUrls.Count > 0)
            {
                imageCount = imageUrls.Count();
                for (var i = 0; i < imageCount; i++)
                {
                    var imageUrl = imageUrls[i];
                    var posterDestinationPath = $"{tempPath}/{i}.jpg";
                    await webClient.DownloadFileTaskAsync(imageUrl, posterDestinationPath);
                }
            }
            //generate a text image
            else
            {
                imageCount = 1;
                var posterDestinationPath = $"{tempPath}/0.jpg";
                if (imageType == ImageType.Poster)
                {
                    this.Utility.CreateTextPoster(this.Title, posterDestinationPath);
                }
                else if (imageType == ImageType.Backdrop)
                {
                    this.Utility.CreateTextBackdrop(this.Title, posterDestinationPath);
                }
            }

            //delete the files in the poster folder path
            if (Directory.Exists(destinationFolderPath))
            {
                Directory.Delete(destinationFolderPath, true);
            }

            //move the tmp files into the poster folder path
            Directory.Move($"{tempPath}", destinationFolderPath);

            //make resized versions of the poster for various devices
            var resizedPosterWidths = new int[] { 100, 200 };

            foreach (var posterWidth in resizedPosterWidths)
            {
                for (var i = 0; i < imageCount; i++)
                {
                    var destinationPath = $"{destinationFolderPath}/{i}w{posterWidth}.jpg";
                    var sourcePosterPath = $"{destinationFolderPath}/{i}.jpg";
                    this.Utility.ResizeImage(sourcePosterPath, destinationPath, posterWidth);
                }
            }
            return imageCount;
        }
    }
}