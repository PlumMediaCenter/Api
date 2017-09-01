using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using Newtonsoft.Json;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    /// <summary>
    /// A singleton library generator 
    /// </summary>
    public class LibraryGenerator
    {
        private LibraryGenerator()
        {
            try
            {
                //load any old status saved in cache
                var statusJson = File.ReadAllText(LibraryGenerator.StatusFilePath);
                this.Status = Newtonsoft.Json.JsonConvert.DeserializeObject<Status>(statusJson);
            }
            catch (Exception) { }
        }
        private static string StatusFilePath
        {
            get
            {
                return $"{AppSettings.TempPath}libraryStatus.json";
            }
        }

        private static LibraryGenerator _Instance;
        public static LibraryGenerator Instance
        {
            get
            {
                return _Instance = _Instance != null ? _Instance : new LibraryGenerator();
            }
        }

        public Manager Manager = new Manager();

        private Status Status;
        public Status GetStatus()
        {
            return this.Status?.Clone();
        }

        private bool IsGenerating = false;
        public async Task Generate()
        {
            try
            {
                if (IsGenerating == true)
                {
                    throw new Exception("Library generation is already in process");
                }
                IsGenerating = true;
                var oldStatus = this.Status;
                this.Status = new Status();
                this.Status.IsProcessing = true;
                this.Status.LastGeneratedDate = oldStatus?.LastGeneratedDate;
                this.Status.State = "processing movies";
                await this.ProcessMovies();
                this.Status.State = "processing tv shows";
                await this.ProcessSeries();
                this.Status.State = "completed";
                this.Status.LastGeneratedDate = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                //find the deepest exception and only keep that
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }
                this.Status.State = "failed";
                this.Status.Error = e;
            }
            finally
            {
                this.Status.IsProcessing = false;
            }
            try
            {
                var json = JsonConvert.SerializeObject(LibraryGenerator.StatusFilePath);
                File.WriteAllText(AppSettings.TempPath, json);
            }
            catch (Exception) { }
            IsGenerating = false;
        }

        private async Task ProcessMovies()
        {
            var moviePaths = new List<MoviePath>();

            var movieSources = await this.Manager.LibraryGeneration.Sources.GetByType("movie");

            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                if (Directory.Exists(source.FolderPath))
                {
                    var directories = Directory.GetDirectories(source.FolderPath).ToList();
                    foreach (var dir in directories)
                    {
                        moviePaths.Add(new MoviePath { Path = dir, Source = source });
                    }
                }
            }

            //find all movies from the db
            var dbMovies = await this.Manager.LibraryGeneration.Movies.GetDirectories();
            foreach (var kvp in dbMovies)
            {
                var source = movieSources.Where(x => x.Id == kvp.Key).First();
                foreach (var path in kvp.Value)
                {
                    moviePaths.Add(new MoviePath { Path = path, Source = source });
                }
            }

            var pathLookup = new Dictionary<string, bool>();
            var distinctList = new List<MoviePath>();
            //remove any duplicates or bogus entries
            foreach (var item in moviePaths)
            {
                //if (pathLookup.ContainsKey(item.Path) == false && item.Path != null)
                if (pathLookup.ContainsKey(item.Path) == false)
                {
                    pathLookup.Add(item.Path, true);
                    distinctList.Add(item);
                }
            }
            moviePaths = distinctList;

            //update Status
            this.Status.MovieCountTotal = moviePaths.Count;
            var random = new Random();
            //process each movie. movie.Process will handle adding, updating, and deleting

            moviePaths.ForEach((moviePath) =>
            // Parallel.ForEach(moviePaths, (moviePath) =>
            {
                var path = moviePath.Path;
                //add this move to the list of currently processing movies
                this.Status.ActiveFiles.Add(path);
                var manager = new Manager();
                var movie = new Movie(manager, moviePath.Path, moviePath.Source.Id.Value);
                try
                {
                    movie.Process().Wait();
                }
                catch (Exception)
                {
                    this.Status.FailedItems.Add(Newtonsoft.Json.JsonConvert.SerializeObject(movie));
                }
                this.Status.MovieCountCurrent++;
                //Thread.Sleep(100);
                //remove the movie from the list of currently processing movies
                this.Status.ActiveFiles.Remove(path);
            });
        }

        private async Task ProcessSeries()
        {
            var seriePaths = new List<string>();

            var serieSources = await this.Manager.LibraryGeneration.Sources.GetByType("tvserie");
            //find all show folders from each source
            foreach (var source in serieSources)
            {
                seriePaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            seriePaths.AddRange(await this.Manager.LibraryGeneration.TvSeries.GetDirectories());

            //remove any duplicates
            seriePaths = seriePaths.Distinct().ToList();

            //process each show. movie.Process will handle adding, updating, and deleting
            Parallel.ForEach(seriePaths, moviePath =>
            {
                var sourceId = 0UL;
                var show = new TvSerie(this.Manager, moviePath, sourceId);
                show.Process();
            });
        }

    }
    class MoviePath
    {
        public string Path;
        public Source Source;
    }

    public class Status
    {
        /// <summary>
        /// The current state ("generating", "generated")
        /// </summary>
        public string State { get; set; }
        public bool IsProcessing { get; set; }
        public Exception Error { get; set; }
        /// <summary>
        /// The end time of the last time the library was generated. This is not updated until a generation has completed.
        /// </summary>
        public DateTime? LastGeneratedDate { get; set; }
        /// <summary>
        /// The total number of movie entries to process
        /// </summary>
        public int MovieCountTotal { get; set; }
        /// <summary>
        /// The current number of movie entries that have been processed
        /// </summary>
        public int MovieCountCurrent { get; set; }
        public List<string> FailedItems { get; set; } = new List<string>();
        /// <summary>
        /// The list of movies currently being processed
        /// </summary>
        /// <returns></returns>
        public List<string> ActiveFiles { get; set; } = new List<string>();

        public Status Clone()
        {
            var clone = (Status)this.MemberwiseClone();
            clone.ActiveFiles = clone.ActiveFiles.ToList();
            return clone;
        }
    }
}