using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class Movie : IProcessable
    {
        public Movie(Manager manager, string moviePath, int sourceId)
        {
            this.Manager = manager;
            this.FolderPath = moviePath;
            this.SourceId = sourceId;
        }
        private Manager Manager;

        /// <summary>
        /// The id for this video. This is only set during Process(), so don't depend on it unless you are calling a function from Process()
        /// </summary>
        private int? Id;
        public int? GetId()
        {
            return this.Id;
        }

        /// <summary>
        /// The id for the video source
        /// </summary>
        public int SourceId;

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
        private string _Md5 { get; set; }

        public int? CompletionSeconds
        {
            get
            {
                return this.MovieDotJson?.CompletionSeconds;
            }
        }

        public string Title
        {
            get
            {
                if (_Title == null)
                {
                    if (string.IsNullOrEmpty(this.MovieDotJson?.Title) == false)
                    {
                        _Title = this.MovieDotJson.Title;
                    }
                    //use the directory name
                    else
                    {
                        var year = GetYearFromFolderName(this.FolderName);
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
                }
                return _Title;
            }
        }
        private string _Title;

        public string SortTitle
        {
            get
            {
                if (string.IsNullOrEmpty(this.MovieDotJson?.SortTitle) == false)
                {
                    return this.MovieDotJson.SortTitle;
                }
                else
                {
                    return this.Title;
                }
            }
        }

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

        public int? RuntimeSeconds
        {
            get
            {
                if (_Runtime == null)
                {
                    var runtimeFromJson = this.MovieDotJson?.RuntimeSeconds;
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
                            _Runtime = (int?)Math.Ceiling(file.Properties.Duration.TotalSeconds);
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
            await this.DownloadMetadataIfPossible();
            //movie needs updated
            if (await this.Manager.LibraryGeneration.Movies.Exists(this.FolderPath))
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

        static Regex YearRegex = new Regex(@"\((\d\d\d\d)\)");
        public static int? GetYearFromFolderName(string folderName)
        {
            try
            {
                var match = YearRegex.Match(folderName);
                var yearString = match.Groups[1]?.Value;
                if (yearString != null)
                {
                    return int.Parse(yearString);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static string NormalizeTitle(string title)
        {
            var replacementChars = new string[] { "{", "}", "#", "@", "-", "(", ")", ":", ".", ",", "'", "?", "!", "+", "$", "’", "…", "/", "_", "[", "]", "–", "*", "=" };
            //force to lower case
            title.ToLowerInvariant()
            //remove starting or trailing spaces
            .Trim();

            //replace lots of special characters with spaces
            foreach (var replacementChar in replacementChars)
            {
                title = title.Replace(replacementChar, " ");
            }

            //replace all instance of double spaces with single spaces
            while (title.Contains("  "))
            {
                title = title.Replace("  ", " ");
            }
            title = title.Replace("&", "and");
            return title;
        }

        /// <summary>
        /// Compare two titles, but remove some special characters and compare case insensitive.
        /// </summary>
        /// <param name="title1"></param>
        /// <param name="title2"></param>
        /// <returns></returns>
        public static bool TitlesAreEquivalent(string title1, string title2)
        {

            title1 = NormalizeTitle(title1);
            title2 = NormalizeTitle(title2);
            if (title1 == title2)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        private string FolderName
        {
            get
            {
                var folderName = new DirectoryInfo(this.FolderPath).Name;
                return folderName;
            }
        }

        public async Task DownloadMetadataIfPossible()
        {
            Console.WriteLine("Download metadata if possible");
            var movieDotJson = this.MovieDotJson;
            var folderName = this.FolderName;
            //the movie doesn't have any metadata. Download some
            if (movieDotJson == null)
            {
                Console.WriteLine($"{folderName}: No movie.json exists");
                var year = GetYearFromFolderName(this.FolderName);
                string title = this.Title;
                Console.WriteLine($"{folderName}: Searching for results");
                //get search results
                var results = await this.Manager.MovieMetadataProcessor.GetSearchResultsAsync(title);
                Console.WriteLine($"{folderName}: Found {results.Count} results");
                var matches = results.Where(x => TitlesAreEquivalent(x.Title, title)).ToList();
                Console.WriteLine($"{folderName}: Found {matches.Count()} where the title matches");
                if (year != null)
                {
                    Console.WriteLine($"{folderName}: Filtering matches by year");
                    matches = matches.Where(x => x.ReleaseDate.Value.Year == year.Value).ToList();
                    Console.WriteLine($"{folderName}: Found {matches.Count()} matches with the same year");
                }
                //if we have any matches left, use the first one
                var match = matches.FirstOrDefault();
                MovieMetadata metadata;
                if (match == null)
                {
                    Console.WriteLine($"{folderName}: No matches found: using generic metadata");
                    metadata = GetGenericMetadata();
                }
                else
                {
                    Console.WriteLine($"{folderName}: Downloading TMDB metadata");
                    metadata = await this.Manager.MovieMetadataProcessor.GetTmdbMetadataAsync(match.TmdbId);
                }
                Console.WriteLine($"{folderName}: Saving metadata to disc");
                await this.Manager.MovieMetadataProcessor.DownloadMetadataAsync(
                    this.FolderPath,
                    Models.Movie.CalculateFolderUrl(this.SourceId, this.FolderName, this.Manager.BaseUrl),
                    metadata
                );
                Console.WriteLine($"{folderName}: Clearing MovieDotJson");
                //clear _MovieDotJson so the next access will load the new one from disk
                this._MovieDotJson = null;
            }
            else
            {
                //the movie already has metadata, so don't download anything 
                Console.WriteLine($"{folderName}: Already has metadata. Skipping metadata retrieval");
                return;
            }
        }

        public MovieMetadata GetGenericMetadata()
        {
            var movieMetadata = new MovieMetadata();
            movieMetadata.Description = null;
            movieMetadata.Rating = null;

            //get the year from the folder name
            var year = GetYearFromFolderName(this.FolderPath);
            DateTime? releaseDate;
            if (year != null)
            {
                releaseDate = new DateTime(year.Value, 1, 1);
                movieMetadata.ReleaseDate = releaseDate;
            }
            //the runtime should be calculated from the video file's length
            movieMetadata.RuntimeSeconds = null;
            movieMetadata.Summary = null;
            movieMetadata.Title = this.Title;
            movieMetadata.SortTitle = this.SortTitle;
            return movieMetadata;
        }

        public async Task<int> Update()
        {
            return await this.Manager.LibraryGeneration.Movies.Update(this);
        }

        public async Task<int> Create()
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

            //the video doesn't have a poster. Create a text-based poster
            if (File.Exists(sourcePosterPath) == false)
            {
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
                var textBackdropGuid = Guid.NewGuid().ToString();
                var backdropPath = $"{this.BackdropFolderPath}{textBackdropGuid}.jpg";
                //the video doesn't have a backdrop. Create a text-based image
                this.Manager.Utility.CreateTextBackdrop(this.Title, backdropPath);
                backdropPaths.Add(backdropPath);
                guidsFromFilesystem.Add(textBackdropGuid);
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