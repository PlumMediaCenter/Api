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
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business
{
    /// <summary>
    /// A singleton library generator. This should only be initialized by the .net DI, and as a singleton
    /// </summary>
    public class LibraryGenerator
    {
        public LibraryGenerator(
            MovieRepository MovieRepository,
            LibGenFactory LibGenFactory,
            SourceRepository SourceRepository,
            LibGenMovieRepository LibGenMovieRepository,
            LibGenTvShowRepository LibGenTvShowRepository,
            SearchCatalog searchCatalog
        )
        {
            this.MovieRepository = MovieRepository;
            this.LibGenFactory = LibGenFactory;
            this.SourceRepository = SourceRepository;
            this.LibGenMovieRepository = LibGenMovieRepository;
            this.LibGenTvShowRepository = LibGenTvShowRepository;
            this.SearchCatalog = searchCatalog;
            try
            {
                if (File.Exists(LibraryGenerator.StatusFilePath))
                {
                    //load any old status saved in cache
                    var statusJson = File.ReadAllText(LibraryGenerator.StatusFilePath);
                    this.Status = Newtonsoft.Json.JsonConvert.DeserializeObject<LibraryGeneratorStatus>(statusJson);
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
        LibGenTvShowRepository LibGenTvShowRepository;
        SearchCatalog SearchCatalog;

        private static string StatusFilePath
        {
            get
            {
                //make sure the temp folder exists
                var path = Utility.NormalizePath($"{AppSettings.TempPath}libraryStatus.json", true);
                return path;
            }
        }

        private LibraryGeneratorStatus Status;
        public LibraryGeneratorStatus GetStatus()
        {
            if (this.Status == null)
            {
                var status = new LibraryGeneratorStatus();
                return status;
            }
            else
            {
                return this.Status.Clone();
            }
        }

        public async Task ProcessItems(IEnumerable<int> mediaItemIds)
        {
            var mediaItems = await this.GetMediaItems(mediaItemIds);
            foreach (var mediaItem in mediaItems)
            {
                await mediaItem.Process();
            }

            //rebuild the search index
            //TODO - update the indexer to do partial updates instead of a full regen
            this.SearchCatalog.GenerateIndexes();
        }

        public async Task<IEnumerable<IProcessable>> GetMediaItems(IEnumerable<int> mediaItemIds)
        {
            var results = new List<IProcessable>();
            //get all the movies
            {
                var models = await this.MovieRepository.GetByIds(mediaItemIds, new[] { "id", "sourceId", "folderPath" });
                foreach (var model in models)
                {
                    var libGenMovie = this.LibGenFactory.BuildMovie(model.GetFolderPath(), model.SourceId);
                    libGenMovie.Id = model.Id;
                    results.Add(libGenMovie);
                }
            }
            var resultIds = results.Select(x => x.Id);
            //fail if there were any items we couldn't find
            var missingItems = mediaItemIds.Where(x => resultIds.Contains(x) == false);
            if (missingItems.Count() > 0)
            {
                throw new Exception($"Could not find items with ids ({string.Join(",", missingItems)})");
            }
            return results;
        }

        private bool IsGenerating = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl">We need the baseUrl to handle metadata fetching for certain media items</param>
        /// <returns></returns>
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
                this.Status = new LibraryGeneratorStatus();
                this.Status.StartTime = DateTime.UtcNow;
                this.Status.IsProcessing = true;
                this.Status.LastGeneratedDate = oldStatus?.LastGeneratedDate;
                this.Status.State = "processing movies";
                await this.ProcessMovies();

                this.Status.State = "processing tv shows";
                await this.ProcessSeries();

                this.Status.State = "generating search indexes";
                this.SearchCatalog.GenerateIndexes();

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
                this.Status.Exception = e;
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
            var sourceItems = new List<SourceItem>();

            var movieSources = await this.SourceRepository.GetByType(MediaType.MOVIE);

            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                if (Directory.Exists(source.FolderPath))
                {
                    var directories = Directory.GetDirectories(source.FolderPath);
                    foreach (var dir in directories)
                    {
                        var normalizedPath = Utility.NormalizePath(dir, false);
                        sourceItems.Add(new SourceItem { Path = normalizedPath, Source = source });
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
                    sourceItems.Add(new SourceItem { Path = normalizedPath, Source = source });
                }
            }

            var pathLookup = new Dictionary<string, bool>();
            var distinctList = new List<SourceItem>();
            //remove any duplicates or bogus entries
            foreach (var sourceItem in sourceItems)
            {
                //if (pathLookup.ContainsKey(item.Path) == false && item.Path != null)
                if (pathLookup.ContainsKey(sourceItem.Path) == false)
                {
                    pathLookup.Add(sourceItem.Path, true);
                    distinctList.Add(sourceItem);
                }
            }
            sourceItems = distinctList;

            //update Status
            this.Status.SetMediaTypeCountTotal(MediaType.MOVIE, sourceItems.Count);
            var random = new Random();


            //process each movie. movie.Process will handle adding, updating, and deleting
            var pool = new SmartThreadPool();
            foreach (var loopMoviePath in sourceItems)
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
                    var movie = this.LibGenFactory.BuildMovie(moviePath.Path, moviePath.Source.Id);
                    try
                    {
                        this.Status.Log.Add($"Waiting for movie to process: {moviePath.Path}");
                        movie.Process().Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing movie {moviePath.Path}");
                        Console.WriteLine(e);
                        this.Status.FailedItems.Add(new FailedItem()
                        {
                            Id = movie.Id,
                            MediaType = MediaType.MOVIE,
                            Path = movie.FolderPath,
                            Exception = e
                        });
                    }
                    Thread.Sleep(5000);
                    this.Status.IncrementMediaTypeCount(MediaType.MOVIE);
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

            var serieSources = await this.SourceRepository.GetByType(MediaType.TV_SHOW);
            //find all show folders from each source
            foreach (var source in serieSources)
            {
                seriePaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            seriePaths.AddRange(await this.LibGenTvShowRepository.GetDirectories());

            //remove any duplicates
            seriePaths = seriePaths.Distinct().ToList();

            var pool = new SmartThreadPool();
            foreach (var loopSeriePath in seriePaths)
            {
                //process each show. show.Process will handle adding, updating, and deleting itself
                pool.QueueWorkItem((seriePath) =>
                {
                    var serie = this.LibGenFactory.BuildTvShow(seriePath, 0);
                    serie.Process().Wait();
                }, loopSeriePath);
            }
        }
    }
    class SourceItem
    {
        public string Path;
        public Source Source;
    }

    public class LibraryGeneratorStatus
    {
        public LibraryGeneratorStatus()
        {
            this.MediaTypeCounts.Add(new MediaTypeCount { MediaType = MediaType.MOVIE });
            this.MediaTypeCounts.Add(new MediaTypeCount { MediaType = MediaType.TV_SHOW });
        }
        /// <summary>
        /// The current state ("generating", "generated")
        /// </summary>
        public string State
        {
            get
            {
                if (_State == null)
                {
                    return "never generated";
                }
                else
                {
                    return _State;
                }
            }
            set
            {
                _State = value;
            }
        }
        private string _State;
        public List<string> Log { get; set; } = new List<string>();
        public bool IsProcessing { get; set; }
        public Exception Exception { get; set; }
        /// <summary>
        /// The end time of the last time the library was generated. This is not updated until a generation has completed.
        /// </summary>
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime? StartTime { get; set; }
        /// <summary>
        /// The list of counts (one for each media type)
        /// </summary>
        /// <returns></returns>
        public List<MediaTypeCount> MediaTypeCounts { get; set; } = new List<MediaTypeCount>();

        public void IncrementMediaTypeCount(MediaType mediaType)
        {
            this.MediaTypeCounts.First(x => x.MediaType == mediaType).Completed++;
        }

        public void SetMediaTypeCountTotal(MediaType mediaType, int total)
        {
            this.MediaTypeCounts.First(x => x.MediaType == mediaType).Total = total;
        }

        /// <summary>
        /// Get the number of items that have not yet been processed
        /// </summary>
        /// <returns></returns>
        public int CountRemaining
        {
            get
            {
                return this.MediaTypeCounts.Sum(x => x.Remaining);
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
                return this.MediaTypeCounts.Sum(x => x.Total);
            }
        }

        /// <summary>
        /// The number of completed items
        /// </summary>
        /// <returns></returns>
        public int CountCompleted
        {
            get
            {
                return this.MediaTypeCounts.Sum(x => x.Completed);
            }
        }
        public int SecondsRemaining
        {
            get
            {
                if (this.CountCompleted == 0 || this.CountTotal == 0)
                {
                    return 0;
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
        public List<FailedItem> FailedItems { get; set; } = new List<FailedItem>();
        /// <summary>
        /// The list of movies currently being processed
        /// </summary>
        /// <returns></returns>
        public List<string> ActiveFiles { get; set; } = new List<string>();

        public LibraryGeneratorStatus Clone()
        {
            var clone = (LibraryGeneratorStatus)this.MemberwiseClone();
            clone.ActiveFiles = clone.ActiveFiles.ToList();
            return clone;
        }
    }

    public class MediaTypeCount
    {
        public MediaType MediaType;
        public int Total { get; set; } = 0;
        public int Completed { get; set; } = 0;
        public int Remaining
        {
            get
            {
                return Total - Completed;
            }
        }
    }


    public class FailedItem
    {
        public int? Id;
        public string Path;
        public MediaType MediaType;
        public Exception Exception;
    }

}