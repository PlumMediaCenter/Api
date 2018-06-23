using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PlumMediaCenter.Business.Factories;
using PlumMediaCenter.Business.Metadata;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace PlumMediaCenter.Business.MetadataProcessing
{
    public class MovieMetadataProcessor
    {
        public MovieMetadataProcessor(
            AppSettings AppSettings,
            Lazy<MovieRepository> LazyMovieRepository,
            LibGenFactory LibGenFactory,
            LibGenMovieRepository LibGenMovieRepository,
            TMDbClient TMDbClient
        )
        {
            this.AppSettings = AppSettings;
            this.LazyMovieRepository = LazyMovieRepository;
            this.LibGenFactory = LibGenFactory;
            this.LibGenMovieRepository = LibGenMovieRepository;
            this.Client = TMDbClient;
        }
        AppSettings AppSettings;
        Lazy<MovieRepository> LazyMovieRepository;
        MovieRepository MovieRepository
        {
            get
            {
                return this.LazyMovieRepository.Value;
            }
        }
        LibGenFactory LibGenFactory;
        LibGenMovieRepository LibGenMovieRepository;

        TMDbClient Client;

        public async Task<List<MovieMetadataSearchResult>> GetSearchResultsAsync(string searchText)
        {
            SearchContainer<TMDbLib.Objects.Search.SearchMovie> r;
            try
            {
                //only allow one tmdb request at a time
                lock (Client)
                {
                    r = Client.SearchMovieAsync(searchText).Result;
                }
            }
            catch (Exception e)
            {
                throw new Exception("TMDB Client was supposed to try again", e);
            }
            var searchResults = r.Results;
            var result = new List<MovieMetadataSearchResult>();
            foreach (var searchResult in searchResults)
            {
                result.Add(new MovieMetadataSearchResult
                {
                    Title = searchResult.Title,
                    PosterUrl = Client.GetImageUrl("original", searchResult.PosterPath).ToString(),
                    TmdbId = searchResult.Id,
                    Overview = searchResult.Overview,
                    ReleaseYear = searchResult.ReleaseDate?.Year
                });
            }
            return await Task.FromResult(result);
        }

        public async Task<MovieMetadataComparison> GetComparisonAsync(int tmdbId, int movieId)
        {
            var result = new MovieMetadataComparison();
            var tcurrent = GetCurrentMetadataAsync(movieId);
            var tTmdb = GetTmdbMetadataAsync(tmdbId);

            //convert current posters into tmdb poster urls

            result.Current = await tcurrent;
            result.Incoming = await tTmdb;
            return result;
        }

        public async Task<MovieMetadata> GetTmdbMetadataAsync(int tmdbId)
        {
            TMDbLib.Objects.Movies.Movie movie = null;
            Directory.CreateDirectory(this.AppSettings.TmdbCacheDirectoryPath);
            var cacheFilePath = $"{this.AppSettings.TmdbCacheDirectoryPath}/{tmdbId}.json";
            //if a cache file exists, and it's was updated less than a month ago, use it.
            if (File.Exists(cacheFilePath) && (DateTime.Now - File.GetLastWriteTime(cacheFilePath)).TotalDays < 30)
            {
                try
                {
                    movie = Newtonsoft.Json.JsonConvert.DeserializeObject<TMDbLib.Objects.Movies.Movie>(File.ReadAllText(cacheFilePath));
                }
                catch (Exception)
                {

                }
            }
            //if the movie could not be loaded from cache, retrieve a fresh copy from TMDB
            if (movie == null)
            {
                //only allow one thread to use the client at a time
                lock (Client)
                {
                    movie = Client.GetMovieAsync(tmdbId,
                       MovieMethods.AlternativeTitles |
                       MovieMethods.Credits |
                       MovieMethods.Images |
                       MovieMethods.Keywords |
                       MovieMethods.Releases |
                       MovieMethods.ReleaseDates |
                       MovieMethods.Videos
                   ).Result;
                }
                //save this result to disc
                var camelCaseFormatter = new JsonSerializerSettings();
                camelCaseFormatter.ContractResolver = new CamelCasePropertyNamesContractResolver();
                camelCaseFormatter.Formatting = Formatting.Indented;

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(movie, camelCaseFormatter);
                await File.WriteAllTextAsync(cacheFilePath, json);
            }

            var metadata = new MovieMetadata();
            metadata.AddCast(movie.Credits?.Cast);
            metadata.AddCrew(movie.Credits?.Crew);
            metadata.Collection = movie.BelongsToCollection?.Name;
            metadata.Summary = movie.Overview;
            metadata.Genres = movie.Genres?.Select(x => x.Name).ToList();
            metadata.Keywords = movie.Keywords?.Keywords?.Select(x => x.Name).ToList();
            var release = movie.Releases?.Countries
                ?.Where(x => x.Iso_3166_1.ToLower() == "us")
                ?.OrderBy(x => x.ReleaseDate)
                ?.First();
            //get the oldest US rating
            metadata.Rating = release?.Certification;
            metadata.ReleaseYear = release?.ReleaseDate?.Year;
            //conver the runtime to seconds
            metadata.RuntimeSeconds = movie.Runtime * 60;
            metadata.Summary = movie.Overview;
            metadata.Title = movie.Title;
            metadata.SortTitle = movie.Title;

            metadata.ExtraSearchText.AddRange(
                movie.AlternativeTitles?.Titles?.Where(x => x.Iso_3166_1.ToLower() == "us").Select(x => x.Title).ToList() ?? new List<string>()
            );
            metadata.ExtraSearchText.Add(movie.OriginalTitle);

            metadata.ExtraSearchText = metadata.ExtraSearchText.Distinct().ToList();

            metadata.TmdbId = movie.Id;


            if (movie.PosterPath != null)
            {
                metadata.PosterUrls.Add(Client.GetImageUrl("original", movie.PosterPath).ToString());
            }
            metadata.PosterUrls.AddRange(
                movie.Images?.Posters
                ?.Where(x => x.Iso_639_1?.ToLower() == "en")
                ?.Select(x => Client.GetImageUrl("original", x.FilePath).ToString())
                ?.ToList() ?? new List<string>()
            );
            metadata.PosterUrls = metadata.PosterUrls.Distinct().ToList();


            //add the marked backdrop path first
            if (movie.BackdropPath != null)
            {
                metadata.BackdropUrls.Add(Client.GetImageUrl("original", movie.BackdropPath).ToString());
            }
            //add all additional backdrops
            metadata.BackdropUrls.AddRange(
                movie.Images?.Backdrops
                //move the highest rated backdrops to the top
                ?.OrderByDescending(x => x.VoteAverage)
                ?.Where(x => x.Iso_639_1?.ToLower() == "en" || x.Iso_639_1 == null)
                ?.Select(x => Client.GetImageUrl("original", x.FilePath).ToString())
                ?.ToList() ?? new List<string>()
            );
            metadata.BackdropUrls = metadata.BackdropUrls.Distinct().ToList();
            return metadata;
        }

        private async Task<MovieMetadata> GetCurrentMetadataAsync(int movieId)
        {
            var movieModel = await this.MovieRepository.GetById(movieId, this.MovieRepository.AllColumnNames);
            var movie = this.LibGenFactory.BuildMovie(movieModel.GetFolderPath(), movieModel.SourceId);
            var metadata = await movie.GetMetadataFromTmdb();

            //if the movie has a poster, add its local url
            var posterPath = $"{movie.FolderPath}/poster.jpg";
            if (File.Exists(posterPath))
            {
                var name = Path.GetFileName(posterPath);
                metadata.PosterUrls.Add($"{movieModel.GetFolderUrl()}{name}");
            }

            //get all backdrops listed in movie.json
            // var backdrops = metadata?.Backdrops ?? new List<Image>();

            // //get all backdrops from filesystem, and include only those not already listed in the movie.json
            // var backdropsFromFs = Directory.Exists(movie.BackdropFolderPath) ? Directory.GetFiles(movie.BackdropFolderPath) : new string[0];
            // foreach (var backdropPath in backdropsFromFs)
            // {
            //     var backdropFilename = Path.GetFileName(backdropPath);
            //     var backdropAlreadyListed = backdrops.Where(x =>
            //     {
            //         return Path.GetFileName(x.Path) == backdropFilename;
            //     }).Count() > 0;

            //     if (backdropAlreadyListed == false)
            //     {
            //         var relativeBackdropPath = $"backdrops/{backdropFilename}";
            //         backdrops.Add(new Image { Path = relativeBackdropPath });
            //     }
            // }

            // foreach (var backdrop in backdrops)
            // {
            //     //add the source url as is
            //     if (backdrop.SourceUrl != null)
            //     {
            //         metadata.BackdropUrls.Add(backdrop.SourceUrl);
            //     }
            //     else
            //     {
            //         //the backdrop doesn't have a source url...so assume it's a locally added image. add the local url
            //         var path = $"{movie.FolderPath}/{backdrop.Path}";
            //         if (File.Exists(path))
            //         {
            //             var name = Path.GetFileName(path);
            //             metadata.BackdropUrls.Add($"{movieModel.GetFolderUrl()}{backdrop.Path}");
            //         }
            //     }
            // }
            return metadata;
        }

        public async Task SaveAsync(int movieId, MovieMetadata metadata)
        {
            var movie = await this.MovieRepository.GetById(movieId, this.MovieRepository.AllColumnNames);
            await DownloadMetadataAsync(movie.GetFolderPath(), movie.GetFolderUrl(), metadata);
            //reprocess this movie so the library is updated with its info
            await this.LibGenMovieRepository.Process(movie.GetFolderPath());
        }

        public async Task DownloadMetadataAsync(string movieFolderPath, string movieFolderUrl, MovieMetadata metadata)
        {
            //process the poster
            {
                var posterPath = $"{movieFolderPath}poster.jpg";

                if (metadata.PosterUrls.Count == 0)
                {
                    if (File.Exists(posterPath))
                    {
                        File.Delete(posterPath);
                    }
                }
                else
                {
                    //only keep the first poster, since we only store a single poster
                    new WebClient().DownloadFile(metadata.PosterUrls.First(), posterPath);
                }
            }

            //copy the backdrops
            CopyBackdrops(metadata, movieFolderUrl, movieFolderPath);

            var movieDotJsonPath = $"{movieFolderPath}movie.json";
            var movieDotJson = new MovieMetadata(metadata);

            var camelCaseFormatter = new JsonSerializerSettings();
            camelCaseFormatter.ContractResolver = new CamelCasePropertyNamesContractResolver();
            camelCaseFormatter.Formatting = Formatting.Indented;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(movieDotJson, camelCaseFormatter);
            await File.WriteAllTextAsync(movieDotJsonPath, json);

        }
        public List<string> CopyBackdrops(MovieMetadata metadata, Models.Movie movie, string moviePath)
        {
            return this.CopyBackdrops(metadata, movie.GetFolderUrl(), moviePath);
        }

        /// <summary>
        /// Copy/download a set of images to the destination path, removing any images from destination that are not in the list.
        /// Returns a list of image paths for the newly copied files
        /// </summary>
        /// <param name="imageUrls"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public List<string> CopyBackdrops(MovieMetadata metadata, string movieFolderUrl, string moviePath)
        {
            return new List<string>();
            // var destinationPath = Utility.NormalizePath($"{moviePath}backdrops/", false);
            // var tempPaths = new List<string>();
            // Directory.CreateDirectory(AppSettings.TempPath);
            // var backdropUrlsToProcess = new List<string>();

            // var originalBackdropUrls = metadata.BackdropUrls;
            // metadata.BackdropUrls = new List<string>();

            // //exclude any backdrops that we already have
            // foreach (var imageUrl in metadata.BackdropUrls)
            // {
            //     var image = originalBackdropUrls.Where(x => x.SourceUrl == imageUrl).FirstOrDefault();
            //     var imagePath = image?.Path == null ? null : Utility.NormalizePath($"{moviePath}{image.Path}", true);
            //     //if this image originated from this url, store a basic image record in the json
            //     if (string.IsNullOrWhiteSpace(this.AppSettings.GetBaseUrl()) == false &&
            //         imageUrl.ToLowerInvariant().Contains(this.AppSettings.GetBaseUrl().ToLowerInvariant()))
            //     {
            //         var len = imageUrl.Length - imageUrl.ToLowerInvariant().Replace(movieFolderUrl.ToLowerInvariant(), "").Length;
            //         var relativePath = imageUrl.Substring(len);

            //         metadata.Backdrops.Add(new Image { Path = relativePath });
            //     }
            //     //if we don't have reference to this image in the json, or the image doesn't exist on disc, process it
            //     else if (image == null || File.Exists(imagePath) == false)
            //     {
            //         //store the backdrop in the list of backdrops (to maintain sort order). This record will be updated
            //         //with a filename later in the process
            //         metadata.Backdrops.Add(new Image { SourceUrl = imageUrl });
            //         backdropUrlsToProcess.Add(imageUrl);
            //     }
            //     else
            //     {
            //         //keep the existing image
            //         metadata.Backdrops.Add(image);
            //     }
            // }

            // //download the new posters
            // foreach (var imageUrl in backdropUrlsToProcess)
            // {
            //     var ext = Path.GetExtension(imageUrl);
            //     var filename = $"{Guid.NewGuid().ToString()}{ Path.GetExtension(imageUrl)}";
            //     var tempImagePath = $"{AppSettings.TempPath}/{filename}";
            //     var client = new WebClient();
            //     Directory.CreateDirectory(AppSettings.TempPath);
            //     client.DownloadFile(imageUrl, tempImagePath);
            //     tempPaths.Add(tempImagePath);

            //     //update metadata with backdrop filename
            //     var imageFromJson = metadata.Backdrops.Where(x => x.SourceUrl == imageUrl).FirstOrDefault();
            //     imageFromJson.Path = Utility.NormalizePath($"backdrops/{filename}", true);
            // }
            // //make the backdrop folder in the movie folder
            // Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            // var imagePaths = new List<string>();
            // //copy all of the temp posters into the backdrops folder
            // foreach (var tempImagePath in tempPaths)
            // {
            //     var filename = Path.GetFileName(tempImagePath);
            //     var imagePath = $"{destinationPath}{filename}";
            //     //copy the image to the destination
            //     File.Copy(tempImagePath, imagePath);
            //     //delete the temp image
            //     File.Delete(tempImagePath);
            //     imagePaths.Add(imagePath);
            // }
            // return imagePaths;
        }
    }

    public class MovieMetadataComparison
    {
        public MovieMetadata Incoming;
        public MovieMetadata Current;
    }

    public class MovieMetadataSearchResult
    {
        public string Title;
        public string PosterUrl;
        public int TmdbId;
        public string Overview;
        public int? ReleaseYear;
    }
}