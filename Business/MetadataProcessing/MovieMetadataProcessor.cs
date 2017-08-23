using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlumMediaCenter.Business.LibraryGeneration.DotJson;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace PlumMediaCenter.Business.MetadataProcessing
{
    class MovieMetadataProcessor
    {
        private TMDbClient Client
        {
            get
            {
                if (_Client == null)
                {
                    _Client = new TMDbClient(new AppSettings().TmdbApiString);
                    //load default config
                    _Client.GetConfig();
                }
                return _Client;
            }
        }
        private TMDbClient _Client;
        public async Task<List<MovieSearchResult>> GetSearchResults(string text)
        {
            var r = await Client.SearchMovieAsync(text);
            var searchResults = r.Results;
            var result = new List<MovieSearchResult>();
            foreach (var searchResult in searchResults)
            {
                result.Add(new MovieSearchResult
                {
                    Title = searchResult.Title,
                    PosterPath = Client.GetImageUrl("original", searchResult.PosterPath).ToString(),
                    Id = searchResult.Id,
                    Overview = searchResult.Overview,
                    ReleaseDate = searchResult.ReleaseDate,
                });
            }
            return result;
        }

        public async Task<MovieMetadataComparison> GetComparison(int tmdbId, int movieId)
        {
            var result = new MovieMetadataComparison();
            var tcurrent = GetCurrentMetadata(movieId);
            var tTmdb = GetTmdbMetadata(tmdbId);

            result.Current = await tcurrent;
            result.Tmdb = await tTmdb;
            return result;
        }

        private async Task<MovieMetadata> GetTmdbMetadata(int tmdbId)
        {
            var movie = await Client.GetMovieAsync(tmdbId,
                MovieMethods.AlternativeTitles |
                MovieMethods.Credits |
                MovieMethods.Images |
                MovieMethods.Keywords |
                MovieMethods.Releases |
                MovieMethods.ReleaseDates |
                MovieMethods.Videos
            );
            var metadata = new MovieMetadata();
            metadata.AddCast(movie.Credits.Cast);
            metadata.AddCrew(movie.Credits.Crew);
            metadata.Collection = movie.BelongsToCollection?.Name;
            metadata.Description = movie.Overview;
            metadata.Genres = movie.Genres?.Select(x => x.Name).ToList();
            metadata.Keywords = movie.Keywords?.Keywords?.Select(x => x.Name).ToList();
            var release = movie.Releases?.Countries?
                .Where(x => x.Iso_3166_1 == "US")?
                .OrderBy(x => x.ReleaseDate)?
                .First();
            //get the oldest US rating
            metadata.Rating = release?.Certification;
            metadata.ReleaseDate = release?.ReleaseDate;
            metadata.Runtime = movie.Runtime;
            metadata.Summary = movie.Overview;
            metadata.Title = movie.Title;
            
            metadata.Titles.Add(movie.Title);
            metadata.Titles.AddRange(
                movie.AlternativeTitles?.Titles?.Where(x => x.Iso_3166_1 == "US").Select(x => x.Title).ToList()
            );
            
            metadata.TmdbId = movie.Id;

            return metadata;
        }

        private async Task<MovieMetadata> GetCurrentMetadata(int movieId)
        {
            var manager = new Manager();
            var movieModel = await manager.Movies.GetById(movieId);
            var movie = new LibraryGeneration.Movie(manager, movieModel.GetFolderPath(), movieModel.SourceId);
            //throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(movie.MovieDotJson));
            var metadata = new MovieMetadata(movie.MovieDotJson);

            var posters = Directory.GetFiles($"{movie.FolderPath}/posters");
            foreach (var poster in posters)
            {
                var name = Path.GetFileName(poster);
                metadata.PosterUrls.Add($"{movieModel.FolderUrl}posters/{name}");
            }

            var backdrops = Directory.GetFiles($"{movie.FolderPath}/backdrops");
            foreach (var backdrop in backdrops)
            {
                var name = Path.GetFileName(backdrop);
                metadata.BackdropUrls.Add($"{movieModel.FolderUrl}backdrops/{name}");
            }
            return metadata;
        }
    }

    public class MovieMetadataComparison
    {
        public MovieMetadata Tmdb;
        public MovieMetadata Current;
    }
    public class MovieMetadata : MovieDotJson
    {
        public MovieMetadata()
        {

        }
        public MovieMetadata(MovieDotJson metadata)
        {
            var t = metadata.GetType();
            var myType = this.GetType();
            var properties = t.GetProperties();
            //set all of the metadata properties to this
            foreach (var prop in properties)
            {
                var value = prop.GetValue(metadata);
                myType.GetProperty(prop.Name).SetValue(this, value);
            }
        }
        public List<string> PosterUrls = new List<string>();
        public List<string> BackdropUrls = new List<string>();
        public void AddCast(List<Cast> cast)
        {
            foreach (var member in cast)
            {
                this.Cast.Add(new CastMember
                {
                    Character = member.Character,
                    Name = member.Name,
                    TmdbId = member.Id
                });
            }
        }
        public void AddCrew(List<Crew> cast)
        {
            foreach (var member in cast)
            {
                this.Crew.Add(new CrewMember
                {
                    Job = member.Job,
                    Name = member.Name,
                    TmdbId = member.Id
                });
            }
        }
    }
    public class MovieSearchResult
    {
        public string Title;
        public string PosterPath;
        public int Id;
        public string Overview;
        public DateTime? ReleaseDate;
    }
}