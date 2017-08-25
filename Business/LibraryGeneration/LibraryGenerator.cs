using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    /// <summary>
    /// A singleton library generator 
    /// </summary>
    public class LibraryGenerator
    {
        private LibraryGenerator()
        {
            this.Manager = new Manager();
        }

        private static LibraryGenerator _Instance;
        public static LibraryGenerator Instance
        {
            get
            {
                return _Instance = _Instance != null ? _Instance : new LibraryGenerator();
            }
        }

        public Manager Manager;

        private Status Status;
        public Status GetStatus()
        {
            return this.Status?.Clone();
        }

        private bool IsGenerating = false;
        public async Task Generate()
        {
            if (IsGenerating == true)
            {
                throw new Exception("Library generation is already in process");
            }
            IsGenerating = true;
            this.Status = new Status();
            this.Status.State = "processing movies";
            await this.ProcessMovies();
            this.Status.State = "processing tv shows";
            await this.ProcessShows();
            this.Status.State = "completed";
            this.Status.LastGeneratedDate = DateTime.UtcNow;
            IsGenerating = false;
        }

        private async Task ProcessMovies()
        {
            var moviePaths = new List<MoviePath>();

            var movieSources = await this.Manager.LibraryGeneration.Sources.GetByType(SourceType.Movie);
            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                var directories = Directory.GetDirectories(source.FolderPath).ToList();
                foreach (var dir in directories)
                {
                    moviePaths.Add(new MoviePath { Path = dir, Source = source });
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
            //remove any duplicates
            foreach (var item in moviePaths)
            {
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
            Parallel.ForEach(moviePaths, (moviePath) =>
            {
                var path = moviePath.Path;
                //add this move to the list of currently processing movies
                this.Status.ActiveFiles.Add(path);
                var movie = new Movie(new Manager(), moviePath.Path, moviePath.Source.Id.Value);
                movie.Process().Wait();
                Thread.Sleep(random.Next(1000, 5000));
                this.Status.MovieCountCurrent++;
                //remove the movie from the list of currently processing movies
                this.Status.ActiveFiles.Remove(path);
            });
        }

        private async Task ProcessShows()
        {
            var showPaths = new List<string>();

            var showSources = await this.Manager.LibraryGeneration.Sources.GetByType(SourceType.Show);
            //find all show folders from each source
            foreach (var source in showSources)
            {
                showPaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            showPaths.AddRange(await this.Manager.LibraryGeneration.Shows.GetDirectories());

            //remove any duplicates
            showPaths = showPaths.Distinct().ToList();

            //process each movie. movie.Process will handle adding, updating, and deleting
            Parallel.ForEach(showPaths, moviePath =>
            {
                var show = new Show(this.Manager, moviePath);
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
        /// <summary>
        /// The end time of the last time the library was generated. This is not updated until a generation has completed.
        /// </summary>
        public DateTime LastGeneratedDate { get; set; }
        /// <summary>
        /// The total number of movie entries to process
        /// </summary>
        public int MovieCountTotal { get; set; }
        /// <summary>
        /// The current number of movie entries that have been processed
        /// </summary>
        public int MovieCountCurrent { get; set; }
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