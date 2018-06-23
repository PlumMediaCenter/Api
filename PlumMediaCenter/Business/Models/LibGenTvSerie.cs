using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Models
{
    public class LibGenTvShow
    {
        public LibGenTvShow(
            string folderPath,
            int sourceId,
            LibGenTvShowRepository libGenTvShowRepository,
            AppSettings appSettings,
            Utility utility,
            TvShowMetadataProcessor tvShowMetadataProcessor
        )
        {
            this.FolderPath = folderPath;
            this.SourceId = sourceId;
            this.LibGenTvShowRepository = libGenTvShowRepository;
            this.AppSettings = appSettings;
            this.Utility = utility;
            this.TvShowMetadataProcessor = tvShowMetadataProcessor;
        }

        public TvShowMetadataProcessor TvShowMetadataProcessor;

        public AppSettings AppSettings;
        public Utility Utility;
        public LibGenTvShowRepository LibGenTvShowRepository { get; set; }
        public int? Id { get; set; }
        public string FolderPath { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Rating { get; set; }
        public int? ReleaseYear { get; set; }
        public int RuntimeSeconds { get; set; }
        public int TmdbId { get; set; }
        public int SourceId { get; set; }
        private string FolderName
        {
            get
            {
                var folderName = new DirectoryInfo(this.FolderPath).Name;
                return folderName;
            }
        }

        public async Task Process()
        {
            Console.WriteLine($"{this.FolderPath}: Process serie");
            //if the serie was deleted, remove it from the system
            if (Directory.Exists(this.FolderPath) == false)
            {
                Console.WriteLine($"{this.FolderPath}: Delete");
                await this.Delete();
                return;
            }
            await this.DownloadMetadataIfPossible();
            //movie needs updated
            if (await this.LibGenTvShowRepository.ExistsInDb(this.FolderPath))
            {
                Console.WriteLine($"{this.FolderPath}: Update");
                this.Id = await this.Update();
            }
            //new movie
            else
            {
                Console.WriteLine($"{this.FolderPath}: Create");
                this.Id = await this.Create();
            }
            // await this.CopyImages();

        }

        public async Task<int> Create()
        {
            return await this.LibGenTvShowRepository.Insert(this);
        }

        public async Task<int> Update()
        {
            return await this.LibGenTvShowRepository.Update(this);
        }

        public async Task DownloadMetadataIfPossible()
        {
            await Task.CompletedTask;
            // Console.WriteLine($"{this.FolderPath}: Download tv show metadata if possible");
            // var folderName = this.FolderName;
            // //the movie doesn't have any metadata. Download some
            // if (tvShowDotJson == null)
            // {
            //     Console.WriteLine($"{FolderPath}: No movie.json exists");
            //     var year = this.Utility.GetYearFromFolderName(this.FolderName);
            //     string title = this.Title;
            //     Console.WriteLine($"{FolderPath}: Searching for results");
            //     //get search results
            //     var results = await this.MovieMetadataProcessor.GetSearchResultsAsync(title);
            //     Console.WriteLine($"{FolderPath}: Found {results.Count} results");
            //     var matches = results.Where(x => this.Utility.TitlesAreEquivalent(x.Title, title)).ToList();
            //     Console.WriteLine($"{FolderPath}: Found {matches.Count()} where the title matches");
            //     if (year != null)
            //     {
            //         Console.WriteLine($"{FolderPath}: Filtering matches by year");
            //         matches = matches.Where((x) =>
            //         {
            //             return x.ReleaseDate != null &&
            //                     year != null &&
            //                     x.ReleaseDate.Value.Year == year.Value;
            //         }).ToList();
            //         Console.WriteLine($"{FolderPath}: Found {matches.Count()} matches with the same year");
            //     }
            //     //if we have any matches left, use the first one
            //     var match = matches.FirstOrDefault();
            //     MovieMetadata metadata;
            //     if (match == null)
            //     {
            //         Console.WriteLine($"{FolderPath}: No matches found: using generic metadata");
            //         metadata = GetGenericMetadata();
            //     }
            //     else
            //     {
            //         Console.WriteLine($"{FolderPath}: Downloading TMDB metadata");
            //         metadata = await this.MovieMetadataProcessor.GetTmdbMetadataAsync(match.TmdbId);
            //     }
            //     Console.WriteLine($"{FolderPath}: Saving metadata to disc");
            //     await this.MovieMetadataProcessor.DownloadMetadataAsync(
            //         this.FolderPath,
            //         Movie.CalculateFolderUrl(this.SourceId, this.FolderName, this.AppSettings.GetBaseUrl()),
            //         metadata
            //     );
            //     Console.WriteLine($"{FolderPath}: Clearing MovieDotJson");
            //     //clear _MovieDotJson so the next access will load the new one from disk
            //     this._TvShowDotJson = null;
            // }
            // else
            // {
            //     //the movie already has metadata, so don't download anything 
            //     Console.WriteLine($"{FolderPath}: Already has metadata. Skipping metadata retrieval");
            //     return;
            // }
        }


        /// <summary>
        /// Delete the movie and all of its related records
        /// </summary>
        /// <returns></returns>
        public async Task Delete()
        {
            this.Id = await this.LibGenTvShowRepository.GetId(this.FolderPath);
            //delete from the database
            await this.LibGenTvShowRepository.Delete(this.FolderPath);

            var imagePaths = new List<string>();
            //delete images from cache
            {
                //poster
                // imagePaths.Add($"{this.AppSettings.PosterFolderPath}{this.Id}.jpg");

                // //backdrops
                // var guids = this.GetBackdropGuidsFromFilesystem();
                // foreach (var guid in guids)
                // {
                //     imagePaths.Add($"{this.AppSettings.BackdropFolderPath}{guid}.jpg");
                // }

                // //delete them
                // foreach (var imagePath in imagePaths)
                // {
                //     if (File.Exists(imagePath))
                //     {
                //         File.Delete(imagePath);
                //     }
                // }
            }
        }
    }
}