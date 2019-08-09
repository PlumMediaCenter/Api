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
            Utility Utility,
            SourceRepository SourceRepository
        )
        {
            this.FolderPath = moviePath;
            this.SourceId = sourceId;

            this.LibGenMovieRepository = LibGenMovieRepository;
            this.MovieMetadataProcessor = MovieMetadataProcessor;
            this.AppSettings = AppSettings;
            this.Utility = Utility;
            this.SourceRepository = SourceRepository;
        }
        LibGenMovieRepository LibGenMovieRepository;
        MovieMetadataProcessor MovieMetadataProcessor;
        AppSettings AppSettings;
        Utility Utility;
        SourceRepository SourceRepository;

        /// <summary>
        /// The id for this video. This is only set during Process(), so don't depend on it unless you are calling a function from Process()
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The source id id for the video's source
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// A full path to the movie folder (excluding the trailing slash)
        /// </summary>
        public string FolderPath
        {
            get
            {
                return this._FolderPath;
            }
            set
            {
                if (value.EndsWith(Path.DirectorySeparatorChar) == true)
                {
                    //remove the trailing slash
                    value = value.Substring(0, value.Length - 1);
                }
                this._FolderPath = value;
            }
        }
        private string _FolderPath;

        public string VideoFileName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.VideoPath);
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
                        _VideoPath = $"{this.FolderPath}{Path.DirectorySeparatorChar}{file.Name}";
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
                    var year = this.Utility.GetYearFromFolderName(this.VideoFileName);
                    if (year != null)
                    {
                        var idx = this.VideoFileName.LastIndexOf($"({year})");
                        if (idx > -1)
                        {
                            _Title = this.VideoFileName.Substring(0, idx).Trim();
                        }
                    }
                    else
                    {
                        _Title = this.VideoFileName;
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

        public IEnumerable<string> PosterPathsFromFileSystem
        {
            get
            {
                if (_PosterPathsFromFileSystem == null)
                {
                    _PosterPathsFromFileSystem = this.Utility.GetPosterPathsForVideo(this.VideoPath);
                }
                return _PosterPathsFromFileSystem;
            }
        }
        private IEnumerable<string> _PosterPathsFromFileSystem;

        public IEnumerable<string> BackdropPathsFromFileSystem
        {
            get
            {
                if (_BackdropPathsFromFileSystem == null)
                {
                    _BackdropPathsFromFileSystem = this.Utility.GetBackdropPathsForVideo(this.VideoPath);

                }
                return _BackdropPathsFromFileSystem;
            }
        }
        private IEnumerable<string> _BackdropPathsFromFileSystem;

        public int? GetYearFromFolderName()
        {
            return this.Utility.GetYearFromFolderName(this.VideoFileName);
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

            var isNewMovie = await this.LibGenMovieRepository.ExistsInDb(this.FolderPath) == false;
            if (isNewMovie)
            {
                await this.ProcessNewMovie();
            }
            else
            {
                await this.ProcessExistingMovie();
            }
        }

        public async Task ProcessNewMovie()
        {
            var record = new DynamicParameters();
            Console.WriteLine($"Inserting basic movie record into db: {this.VideoPath}");
            //insert a basic record so we can get an ID
            this.Id = await this.LibGenMovieRepository.InsertBasic(this);
            try
            {
                Console.WriteLine($"Fetching TMDB metadata for {this.VideoPath}");

                //fetch metadata for this movie once, only if it's a new movie, and only keep an exact match for title and release year
                MovieMetadata metadata = await this.GetMetadataFromTmdb();

                //if we have metadata, use that info 
                if (metadata != null)
                {
                    record.Add("title", metadata.Title);
                    record.Add("sortTitle", metadata.SortTitle);
                    record.Add("rating", metadata.Rating);
                    record.Add("releaseYear", metadata.ReleaseYear);
                    record.Add("summary", metadata.Summary);
                    record.Add("tmdbId", metadata.TmdbId);
                }
                //we don't have metadata...set some defaults
                else
                {
                    record.Add("title", this.Title);
                    record.Add("sortTitle", this.SortTitle);
                    var year = this.GetYearFromFolderName();
                    if (year != null)
                    {
                        record.Add("releaseYear", year);
                    }
                }
                Console.WriteLine($"Copying posters for {this.VideoPath}");
                //use posters from video folder if possible, fallback to downloading them from metadata
                var posterUrls = PosterPathsFromFileSystem.Count() > 0 ? PosterPathsFromFileSystem.ToList() : metadata?.PosterUrls;
                var posterCount = await CopyImages(posterUrls, this.PosterFolderPath, ImageType.Poster);

                Console.WriteLine($"Copying backdrops for {this.VideoPath}");
                //use backdrops from video folder if possible, fallback to downloading them from metadata
                var backdropUrls = this.BackdropPathsFromFileSystem.Count() > 0 ? this.BackdropPathsFromFileSystem.ToList() : metadata?.BackdropUrls;
                var backdropCount = await CopyImages(backdropUrls, this.BackdropFolderPath, ImageType.Backdrop);

                record.Add("posterCount", posterCount);
                record.Add("backdropCount", backdropCount);
                record.Add("id", this.Id);
                record.Add("folderPath", this.FolderPath);
                record.Add("videoPath", this.VideoPath);
                record.Add("runtimeSeconds", this.GetRuntimeSeconds());
                record.Add("sourceId", this.SourceId);

                //update the db with all of the fields we collected
                Console.WriteLine($"Saving enhanced movie info for {this.VideoPath}");
                await this.LibGenMovieRepository.Update(record);
            }
            catch
            {
                //delete the video record...it's just easier to reprocess from scratch once the video has been fixed
                await this.Delete();
                throw;
            }
        }

        public async Task ProcessExistingMovie(MovieMetadata metadata = null)
        {
            var record = new DynamicParameters();
            //this is an existing movie. Fetch its id
            await this.LoadId();

            //if we have metadata, use that info 
            if (metadata != null)
            {
                record.Add("title", metadata.Title);
                record.Add("sortTitle", metadata.SortTitle);
                record.Add("rating", metadata.Rating);
                record.Add("releaseYear", metadata.ReleaseYear);
                record.Add("summary", metadata.Summary);
                record.Add("tmdbId", metadata.TmdbId);
            }

            List<string> posterUrls = null;
            if (metadata != null && metadata.PosterUrls.Count() > 0)
            {
                posterUrls = metadata.PosterUrls;
            }
            else if (PosterPathsFromFileSystem.Count() > 0)
            {
                posterUrls = PosterPathsFromFileSystem.ToList();
            }
            else
            {
                // leave posters the way they are.
            }

            //only copy posters if we have a list of urls to copy
            if (posterUrls != null)
            {
                var posterCount = await CopyImages(posterUrls, this.PosterFolderPath, ImageType.Poster);
                record.Add("posterCount", posterCount);
            }

            List<string> backdropUrls = null;
            if (metadata != null && metadata.BackdropUrls.Count() > 0)
            {
                backdropUrls = metadata.BackdropUrls;
            }
            else if (PosterPathsFromFileSystem.Count() > 0)
            {
                backdropUrls = BackdropPathsFromFileSystem.ToList();
            }
            else
            {
                // leave backdrops the way they are.
            }

            //only copy backdrops if we have a list of backdrops to copy
            if (backdropUrls != null)
            {
                var backdropCount = await CopyImages(backdropUrls, this.BackdropFolderPath, ImageType.Backdrop);
                record.Add("backdropCount", backdropCount);
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
            var year = this.Utility.GetYearFromFolderName(this.VideoFileName);
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

        private async Task<int> CopyImages(List<string> imageUrls, string destinationFolderPath, ImageType imageType)
        {
            imageUrls = imageUrls ?? new List<string>();
            //TODO - temporarily just download the first image in the list
            imageUrls = imageUrls.Take(3).ToList();
            var webClient = new WebClient();
            var imageCount = 0;
            var tempPath = $"{this.CachePath}/tmp/{Guid.NewGuid()}";

            //create the temp directory
            Directory.CreateDirectory(tempPath);

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


            var maxRetries = 3;
            //try several times to move the files from temp to the web cache directory
            for (var i = 0; i < maxRetries; i++)
            {
                if (i > 0)
                {
                    Console.WriteLine("Retrying moving files from \"{tempPath}\" to \"{destinationFolderPath}\"");
                }
                //delete the files in the poster folder path
                try { Directory.Delete(destinationFolderPath, true); } catch { }
                //move the tmp files into the poster folder path
                try
                {
                    Directory.Move($"{tempPath}", destinationFolderPath);
                    //escape the for loop if the file moving worked
                    break;
                }
                catch
                {
                    Console.WriteLine($"Encountered exception when moving files from \"{tempPath}\" to \"{destinationFolderPath}\"");
                    //delay for a small amount of time
                    await Task.Delay(1000);
                    //if we failed enough times while trying to copy the images, hard-fail
                    if (i == maxRetries - 1)
                    {
                        throw;
                    }
                }
            }
            //TODO - figure out why this is here
            // var suffix = "";
            // if (imageType == ImageType.Poster)
            // {
            //     suffix = "";
            // }
            // else if (imageType == ImageType.Backdrop)
            // {
            //     suffix = "-fanart";
            // }

            //make resized versions of the poster for various devices and put them in the web cache directory
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

    public enum ImageType
    {
        Poster = 1,
        Backdrop = 2
    };

}