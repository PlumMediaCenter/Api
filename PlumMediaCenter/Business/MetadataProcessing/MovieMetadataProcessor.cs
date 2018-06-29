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
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new Exception("Search text cannot be empty");
            }
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
            var model = await this.MovieRepository.GetById(movieId, this.MovieRepository.AllColumnNames);

            var metadata = new MovieMetadata();

            metadata.BackdropUrls = model.BackdropUrls.ToList();
            metadata.CompletionSeconds = model.CompletionSeconds;
            metadata.PosterUrls = model.PosterUrls.ToList();
            metadata.Rating = model.Rating;
            metadata.ReleaseYear = model.ReleaseYear;
            metadata.RuntimeSeconds = model.RuntimeSeconds;
            metadata.ShortSummary = model.ShortSummary;
            metadata.SortTitle = model.SortTitle;
            metadata.Summary = model.Summary;
            metadata.Title = model.Title;
            metadata.TmdbId = model.TmdbId;

            return metadata;
        }

        public async Task SaveAsync(int movieId, MovieMetadata metadata)
        {
            var movie = await this.MovieRepository.GetById(movieId, this.MovieRepository.AllColumnNames);
            //reprocess this movie so the library is updated with its info
            await this.LibGenMovieRepository.Process(movie.GetFolderPath(), metadata);
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