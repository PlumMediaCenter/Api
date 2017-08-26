using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
                    PosterUrl = Client.GetImageUrl("original", searchResult.PosterPath).ToString(),
                    TmdbId = searchResult.Id,
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

            //convert current posters into tmdb poster urls

            result.Current = await tcurrent;
            result.Incoming = await tTmdb;
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
            metadata.AddCast(movie.Credits?.Cast);
            metadata.AddCrew(movie.Credits?.Crew);
            metadata.Collection = movie.BelongsToCollection?.Name;
            metadata.Description = movie.Overview;
            metadata.Genres = movie.Genres?.Select(x => x.Name).ToList();
            metadata.Keywords = movie.Keywords?.Keywords?.Select(x => x.Name).ToList();
            var release = movie.Releases?.Countries
                ?.Where(x => x.Iso_3166_1.ToLower() == "us")
                ?.OrderBy(x => x.ReleaseDate)
                ?.First();
            //get the oldest US rating
            metadata.Rating = release?.Certification;
            metadata.ReleaseDate = release?.ReleaseDate;
            metadata.Runtime = movie.Runtime;
            metadata.Summary = movie.Overview;
            metadata.Title = movie.Title;

            metadata.Titles.Add(movie.Title);
            metadata.Titles.AddRange(
                movie.AlternativeTitles?.Titles?.Where(x => x.Iso_3166_1.ToLower() == "us").Select(x => x.Title).ToList() ?? new List<string>()
            );
            metadata.Titles = metadata.Titles.Distinct().ToList();

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


            if (movie.BackdropPath != null)
            {
                metadata.BackdropUrls.Add(Client.GetImageUrl("original", movie.BackdropPath).ToString());
            }
            metadata.BackdropUrls.AddRange(
                movie.Images?.Backdrops
                ?.Where(x => x.Iso_639_1?.ToLower() == "en")
                ?.Select(x => Client.GetImageUrl("original", x.FilePath).ToString())
                ?.ToList() ?? new List<string>()
            );
            metadata.BackdropUrls = metadata.BackdropUrls.Distinct().ToList();

            return metadata;
        }

        private async Task<MovieMetadata> GetCurrentMetadata(int movieId)
        {
            var manager = new Manager();
            var movieModel = await manager.Movies.GetById(movieId);
            var movie = new LibraryGeneration.Movie(manager, movieModel.GetFolderPath(), movieModel.SourceId);
            //throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(movie.MovieDotJson));
            var metadata = new MovieMetadata(movie.MovieDotJson);

            //if the movie has a poster, add its local url
            var posterPath = $"{movie.FolderPath}/poster.jpg";
            if (File.Exists(posterPath))
            {
                var name = Path.GetFileName(posterPath);
                metadata.PosterUrls.Add($"{movieModel.FolderUrl}{name}");
            }

            var backdrops = movie.MovieDotJson?.Backdrops ?? new List<Image>();
            foreach (var backdrop in backdrops)
            {
                //add the source url as is
                if (backdrop.SourceUrl != null)
                {
                    metadata.BackdropUrls.Add(backdrop.SourceUrl);
                }
                else
                {
                    //the poster doesn't have a source url...so assume it's a locally added image. add the local url
                    var path = $"{movie.FolderPath}/{backdrop.Path}";
                    if (File.Exists(path))
                    {
                        var name = Path.GetFileName(path);
                        metadata.BackdropUrls.Add($"{movieModel.FolderUrl}{backdrop.Path}");
                    }
                }
            }
            return metadata;
        }

        public async Task Save(int movieId, MovieMetadata metadata)
        {
            //overwrite the MovieDotJson for this movie
            var manager = new Manager();
            var movie = await manager.Movies.GetById(movieId);

            //process the poster
            {
                //delete any existing poster
                var posterPath = $"{movie.GetFolderPath()}/poster.jpg";
                if (File.Exists(posterPath))
                {
                    File.Delete(posterPath);
                }
                if (metadata.PosterUrls.Count > 0)
                {
                    //only keep the first poster, since we only store a single poster
                    new WebClient().DownloadFile(metadata.PosterUrls.First(), posterPath);
                }
            }

            //copy the backdrops
            CopyImages(metadata.BackdropUrls, $"{movie.GetFolderPath()}/backdrops/");

            var movieDotJsonPath = $"{movie.GetFolderPath()}movie.json";
            var movieDotJson = (MovieDotJson)metadata;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(movieDotJson, Formatting.Indented);
            await File.WriteAllTextAsync(movieDotJsonPath, json);

        }

        /// <summary>
        /// Copy/download a set of images to the destination path, removing any images from destination that are not in the list.
        /// Returns a list of image paths for the newly copied files
        /// </summary>
        /// <param name="imageUrls"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public List<string> CopyImages(List<string> imageUrls, string destinationPath)
        {

            var tempPaths = new List<string>();
            //copy all of the posters 
            Parallel.ForEach(imageUrls, (imageUrl) =>
            {
                var ext = Path.GetExtension(imageUrl);
                var filename = Guid.NewGuid().ToString();
                var tempImagePath = $"{AppSettings.TempPath}/{filename}{ext}";
                var client = new WebClient();
                client.DownloadFile(imageUrl, tempImagePath);
                tempPaths.Add(tempImagePath);
            });
            //make the backdrop folder in the movie folder (if it doesn't already exist)
            if (imageUrls.Count > 0 && Directory.Exists(destinationPath) == false)
            {
                Directory.CreateDirectory(destinationPath);
            }

            //delete all  files from the backdrops folder
            Utility.EmptyDirectory(destinationPath);

            var imagePaths = new List<string>();
            //copy all of the temp posters into the backdrops folder
            Parallel.ForEach(tempPaths, (tempImagePath) =>
            {
                var filename = Path.GetFileName(tempImagePath);
                var imagePath = $"{destinationPath}{filename}";
                //copy the image to the destination
                File.Copy(tempImagePath, imagePath);
                //delete the temp image
                File.Delete(tempImagePath);
                imagePaths.Add(imagePath);
            });
            return imagePaths;
        }
    }

    public class MovieMetadataComparison
    {
        public MovieMetadata Incoming;
        public MovieMetadata Current;
    }
    public class MovieMetadata : MovieDotJson
    {
        public MovieMetadata()
        {

        }
        public MovieMetadata(MovieDotJson metadata)
        {
            if (metadata == null)
            {
                return;
            }
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
            if (cast == null)
            {
                return;
            }
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
        public void AddCrew(List<Crew> crew)
        {
            if (crew == null)
            {
                return;
            }
            foreach (var member in crew)
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
        public string PosterUrl;
        public int TmdbId;
        public string Overview;
        public DateTime? ReleaseDate;
    }
}