using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using Newtonsoft.Json;
using Amib.Threading;
using Dapper;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Business.Factories;

namespace PlumMediaCenter.Business
{
    /// <summary>
    /// A singleton library generator 
    /// </summary>
    public class LibraryGenerator
    {
        public LibraryGenerator(
            MovieRepository MovieRepository,
            LibGenFactory LibGenFactory,
            SourceRepository SourceRepository,
            LibGenMovieRepository LibGenMovieRepository,
            LibGenTvSerieRepository LibGenTvSerieRepository
        )
        {
            this.MovieRepository = MovieRepository;
            this.LibGenFactory = LibGenFactory;
            this.SourceRepository = SourceRepository;
            this.LibGenMovieRepository = LibGenMovieRepository;
            this.LibGenTvSerieRepository = LibGenTvSerieRepository;
            try
            {
                if (File.Exists(LibraryGenerator.StatusFilePath))
                {
                    //load any old status saved in cache
                    var statusJson = File.ReadAllText(LibraryGenerator.StatusFilePath);
                    this.Status = Newtonsoft.Json.JsonConvert.DeserializeObject<Status>(statusJson);
                }
            }
            catch (Exception)
            {
            }
        }
        MovieRepository MovieRepository;
        LibGenFactory LibGenFactory;
        SourceRepository SourceRepository;
        LibGenMovieRepository LibGenMovieRepository;
        LibGenTvSerieRepository LibGenTvSerieRepository;

        private static string StatusFilePath
        {
            get
            {
                //make sure the temp folder exists
                var path = Utility.NormalizePath($"{AppSettings.TempPath}libraryStatus.json", true);
                return path;
            }
        }

        private Status Status;
        public Status GetStatus()
        {
            return this.Status?.Clone();
        }

        public async Task<IProcessable> GetMediaItem(int mediaItemId)
        {
            // var manager = new Manager(AppSettings.BaseUrlStatic);
            var rows = await ConnectionManager.QueryAsync<MediaTypeId>(@"
                select mediaTypeId
                from MediaItemIds 
                where id = @id
            ", new
            {
                id = mediaItemId
            });
            var mediaTypeId = rows.FirstOrDefault();
            switch (mediaTypeId)
            {
                case MediaTypeId.Movie:
                    var movieModel = await this.MovieRepository.GetById(mediaItemId);
                    var movie = this.LibGenFactory.BuildMovie(movieModel.GetFolderPath(), movieModel.SourceId);
                    return movie;
                default:
                    throw new Exception($"{mediaTypeId} Not implemented");
            }
        }

        private bool IsGenerating = false;
        public async Task Generate(string baseUrl)
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
                this.Status.StartTime = DateTime.UtcNow;
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
                var json = JsonConvert.SerializeObject(this.Status);
                File.WriteAllText(LibraryGenerator.StatusFilePath, json);
            }
            catch (Exception)
            {
            }
            IsGenerating = false;
        }

        private async Task ProcessMovies()
        {
            var moviePaths = new List<MoviePath>();

            var movieSources = await this.SourceRepository.GetByType(MediaTypeId.Movie);

            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                if (Directory.Exists(source.FolderPath))
                {
                    var directories = Directory.GetDirectories(source.FolderPath);
                    foreach (var dir in directories)
                    {
                        var normalizedPath = Utility.NormalizePath(dir, false);
                        moviePaths.Add(new MoviePath { Path = normalizedPath, Source = source });
                    }
                }
            }

            //find all movies from the db
            var dbMovies = await this.LibGenMovieRepository.GetDirectories();
            foreach (var kvp in dbMovies)
            {
                var source = movieSources.Where(x => x.Id == kvp.Key).First();
                foreach (var path in kvp.Value)
                {
                    var normalizedPath = Utility.NormalizePath(path, false);
                    moviePaths.Add(new MoviePath { Path = normalizedPath, Source = source });
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
            var pool = new SmartThreadPool();
            foreach (var loopMoviePath in moviePaths)
            {
                this.Status.Log.Add($"Adding {loopMoviePath.Path} to pool");
                var workItemResult = pool.QueueWorkItem((moviePath) =>
                {
                    //temp lock to process movies one by one
                    // lock (this)
                    // {
                    this.Status.Log.Add($"Processing pool movie: {moviePath.Path}");
                    var path = moviePath.Path;
                    //add this move to the list of currently processing movies
                    lock (this.Status.ActiveFiles)
                    {
                        this.Status.ActiveFiles.Add(path);
                    }
                    var movie = this.LibGenFactory.BuildMovie(moviePath.Path, moviePath.Source.Id.Value);
                    try
                    {
                        this.Status.Log.Add($"Waiting for movie to process: {moviePath.Path}");
                        try
                        {
                            movie.Process().Wait();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error processing movie {moviePath.Path}");
                            Console.WriteLine(e);
                            throw e;
                        }
                    }
                    catch (Exception e)
                    {
                        this.Status.FailedItems.Add(JsonConvert.SerializeObject(movie) + " " + JsonConvert.SerializeObject(e));
                    }
                    Thread.Sleep(5000);
                    this.Status.MovieCountCompleted++;
                    //remove the movie from the list of currently processing movies
                    lock (this.Status.ActiveFiles)
                    {
                        this.Status.ActiveFiles.Remove(path);
                    }
                    // }
                }, loopMoviePath);

            }
            //Wait for all work items to complete. equivalent to Thread.WaitAll()
            pool.WaitForIdle();
        }
        private async Task ProcessSeries()
        {
            var seriePaths = new List<string>();

            var serieSources = await this.SourceRepository.GetByType(MediaTypeId.TvShow);
            //find all show folders from each source
            foreach (var source in serieSources)
            {
                seriePaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            seriePaths.AddRange(await this.LibGenTvSerieRepository.GetDirectories());

            //remove any duplicates
            seriePaths = seriePaths.Distinct().ToList();

            var pool = new SmartThreadPool();
            foreach (var loopSeriePath in seriePaths)
            {
                //process each show. movie.Process will handle adding, updating, and deleting
                pool.QueueWorkItem((seriePath) =>
               {
                   var show = this.LibGenFactory.BuildTvSerie(seriePath, 0);
                   show.Process();
               }, loopSeriePath);
            }
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
        public List<string> Log { get; set; } = new List<string>();
        public bool IsProcessing { get; set; }
        public Exception Error { get; set; }
        /// <summary>
        /// The end time of the last time the library was generated. This is not updated until a generation has completed.
        /// </summary>
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime? StartTime { get; set; }
        /// <summary>
        /// The total number of movie entries to process
        /// </summary>
        public int MovieCountTotal { get; set; }
        /// <summary>
        /// The current number of movie entries that have been processed
        /// </summary>
        public int MovieCountCompleted { get; set; }

        public int TvSerieCountTotal { get; set; }
        public int TvSerieCountCompleted { get; set; }

        /// <summary>
        /// The total number of items that have already been processed
        /// </summary>
        /// <returns></returns>
        public int CountCompleted
        {
            get
            {
                return this.MovieCountCompleted + this.TvSerieCountCompleted;
            }
        }

        /// <summary>
        /// Get the number of items that still need to be processed
        /// </summary>
        /// <returns></returns>
        public int CountRemaining
        {
            get
            {
                return this.CountTotal - this.CountCompleted;
            }
        }

        /// <summary>
        /// The total number of items to process
        /// </summary>
        /// <returns></returns>
        public int CountTotal
        {
            get
            {
                return this.MovieCountTotal + this.TvSerieCountTotal;
            }
        }
        public int? SecondsRemaining
        {
            get
            {
                if (this.CountCompleted == 0 || this.CountTotal == 0)
                {
                    return null;
                }
                else
                {
                    var timeTaken = DateTime.UtcNow - this.StartTime;
                    //calculate seconds per item
                    var millisecondsPerItem = timeTaken.Value.TotalMilliseconds / this.CountCompleted;
                    var millisecondsremaining = this.CountRemaining * millisecondsPerItem;
                    var secondsRemaining = millisecondsremaining / 1000;
                    return (int)secondsRemaining;
                }
            }
        }
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