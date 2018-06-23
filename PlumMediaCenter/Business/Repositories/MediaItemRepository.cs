using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Data;
using PlumMediaCenter.Models;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Data;
using System.Data;
using PlumMediaCenter.Business.Models;

namespace PlumMediaCenter.Business.Repositories
{
    public class MediaItemRepository
    {
        public MediaItemRepository(
            AppSettings appSettings,
            Lazy<MovieRepository> lazyMovieRepository,
            SearchCatalog searchCatalog
        )
        {
            this.AppSettings = appSettings;
            this.LazyMovieRepository = lazyMovieRepository;
            this.SearchCatalog = searchCatalog;

        }
        AppSettings AppSettings;
        Lazy<MovieRepository> LazyMovieRepository;
        MovieRepository MovieRepository
        {
            get
            {
                return LazyMovieRepository.Value;
            }
        }
        SearchCatalog SearchCatalog;

        public async Task<MediaItemProgress> SetProgress(int profileId, int mediaItemId, int progressSeconds)
        {
            //get the last progress record for this user and item
            var progress = (await ConnectionManager.QueryAsync<MediaItemProgress>(@"
                select * from MediaItemProgress
                where profileId = @profileId and mediaItemId = @mediaItemId
                order by dateEnd desc
                limit 1
            ", new
            {
                profileId = profileId,
                mediaItemId = mediaItemId
            })).FirstOrDefault();
            //if we have a progress object, see if it's close enough to our new progress to be merged
            if (progress != null)
            {
                var dateSecondsDifference = (DateTime.UtcNow - progress.DateEnd).TotalSeconds;
                var progressSecondsDifference = (double)(progressSeconds - progress.ProgressSecondsEnd);
                var diffBetweenThem = Math.Abs(dateSecondsDifference - progressSecondsDifference);
                if (diffBetweenThem <= this.AppSettings.MaxMediaProgressGapSeconds)
                {
                    progress.ProgressSecondsEnd = progressSeconds;
                    progress.DateEnd = DateTime.UtcNow;
                    await this.ReplaceMediaProgress(progress);
                    return progress;
                }
                else
                {
                    //the gap is outside of the acceptable limit, we need to start a new progress
                    progress = null;
                }
            }
            //only make it to here if we need to make a new progress item.

            //create a new progress item
            return await this.InsertMediaProgress(new MediaItemProgress
            {
                ProfileId = profileId,
                MediaItemId = mediaItemId,
                ProgressSecondsBegin = progressSeconds,
                ProgressSecondsEnd = progressSeconds,
                DateBegin = DateTime.UtcNow,
                DateEnd = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Create a new MediaProrgess record
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task<MediaItemProgress> InsertMediaProgress(MediaItemProgress progress)
        {
            progress.Id = (await ConnectionManager.QueryAsync<int>(@"
                insert into MediaItemProgress(profileId, mediaItemId, progressSecondsBegin,progressSecondsEnd, dateBegin, dateEnd)
                values(@profileId, @mediaItemId, @progressSecondsBegin, @progressSecondsEnd, @dateBegin, @dateEnd);
                select last_insert_id();
            ", progress)).FirstOrDefault();
            return progress;
        }

        /// <summary>
        /// Replace a MediaItemProgress record in the database with the record provided
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task ReplaceMediaProgress(MediaItemProgress progress)
        {
            await ConnectionManager.ExecuteAsync(@"
                update MediaItemProgress
                set 
                    profileId=@profileId,
                    mediaItemId=@mediaItemId, 
                    progressSecondsBegin=@progressSecondsBegin,
                    progressSecondsEnd=@progressSecondsEnd,
                    dateBegin=@dateBegin, 
                    dateEnd=@dateEnd
                where id=@id
            ", progress);
        }

        /// <summary>
        /// Compute the progressSeconds for each specified media item.
        /// The results will be set on the media items directly.
        /// </summary>
        /// <param name="profileId"></param>
        /// <param name="mediaItems"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Movie>> FetchProgressSeconds(int profileId, IEnumerable<Movie> mediaItems)
        {
            var mediaItemIds = mediaItems.Select(x => x.Id);
            IEnumerable<MediaItemProgress> latestProgressRecords;
            using (var connection = ConnectionManager.CreateConnection())
            {
                //get the latest progress record for each media item
                latestProgressRecords = await connection.QueryAsync<MediaItemProgress>(@"
                    select outside.*, outside.mediaItemId, outside.progressSecondsEnd 
                    from MediaItemProgress outside
                    where outside.id in (
                        select id from (
                            select max(id) as id, max(dateEnd)
                            from MediaItemProgress
                            where profileId = @profileId
                            and mediaItemId in @mediaItemIds
                            group by mediaItemId
                        ) as grouper
                    );
                "
                 , new { profileId = profileId, mediaItemIds = mediaItemIds });
            }

            //set progress for each media item (either by the record or by zeroing it out)
            foreach (var mediaItem in mediaItems)
            {
                //get the movie with this progress's id
                var progressRecord = latestProgressRecords.Where(x => x.MediaItemId == mediaItem.Id).FirstOrDefault();
                if (progressRecord != null)
                {
                    //set the progress on the movie
                    mediaItem.ProgressSeconds = progressRecord.ProgressSecondsEnd;
                }
                else
                {
                    //zero out the progress seconds since there is no progress record
                    mediaItem.ProgressSeconds = 0;
                }
            }
            return mediaItems;
        }

        /// <summary>
        /// Get a full list of all history for the specified media item
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryForMediaItem(int profileId, int mediaItemId, int limit = 50, int index = 0)
        {
            var ids = (await ConnectionManager.QueryAsync<int>(@"
                select id from MediaItemProgress
                where profileId = @profileId
                and mediaItemId = @mediaItemId
                order by dateEnd desc
                limit @limit offset @index
            ", new
            {
                profileId = profileId,
                mediaItemId = mediaItemId,
                limit = limit,
                index = index
            }));
            return await this.GetHistoryByIds(ids);
        }

        public async Task<IEnumerable<MediaHistoryRecord>> GetHistory(int profileId, int limit = 50, int index = 0)
        {
            var ids = (await ConnectionManager.QueryAsync<int>(@"
                select id from MediaItemProgress
                where profileId = @profileId
                order by dateEnd desc
                limit @limit offset @index
            ", new
            {
                profileId = profileId,
                index = index,
                limit = limit
            }));
            return await this.GetHistoryByIds(ids);
        }


        /// <summary>
        /// Get a list of history records by an id list
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryByIds(IEnumerable<int> ids)
        {
            if (ids.Count() == 0)
            {
                return new List<MediaHistoryRecord>();
            }
            var items = (await ConnectionManager.QueryAsync<MediaHistoryRecord>(@"
                select *, MediaItemProgress.id as id, MediaItemIds.mediaTypeId as mediaType
                from MediaItemProgress, MediaItemIds
                where MediaItemProgress.mediaItemId = MediaItemIds.id
                  and MediaItemProgress.id in @ids
                order by dateEnd desc
            ", new
            {
                ids = ids
            }));
            //get all of the movies for these media items
            var movieIds = items.Where(x => x.MediaType == MediaType.MOVIE).Select(x => x.MediaItemId);
            var movies = await this.MovieRepository.GetByIds(movieIds, new[] { "id", "posterUrls", "runtimeSeconds", "title" });
            foreach (var item in items.Where(x => x.MediaType == MediaType.MOVIE))
            {
                var movie = movies.Where(x => x.Id == item.MediaItemId).FirstOrDefault();
                if (movie == null)
                {
                    throw new Exception($"Unable to find media item with id {item.MediaItemId}");
                }
                item.PosterUrl = movie.PosterUrls.First();
                item.RuntimeSeconds = movie.RuntimeSeconds;
                item.Title = movie.Title;
            }
            return items;
        }

        public async Task DeleteHistoryRecord(int id)
        {
            await ConnectionManager.ExecuteAsync(@"
                delete from MediaItemProgress
                where id = @id
            ", new { id = id });
        }

        public async Task<int> GetNewMediaId(MediaType mediaType)
        {
            var rows = await ConnectionManager.QueryAsync<int?>(@"
                insert into MediaItemIds(mediaTypeId) values(@mediaTypeId);select last_insert_id();
            ", new { mediaTypeId = (int)mediaType });

            var id = rows.FirstOrDefault();

            if (id == null || id.Value == 0)
            {
                throw new Exception("id cannot be null");
            }
            return id.Value;
        }

        public class GetMediaItemRow
        {
            public int Id { get; set; }
            public MediaType MediaType { get; set; }

        }

        /// <summary>
        /// Get the specific model by its media id
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<object>> GetByIds(IEnumerable<int> mediaItemIds)
        {
            var ids = mediaItemIds.Distinct().ToList();

            //get the media type id from the db.
            var mediaTypes = await ConnectionManager.QueryAsync<GetMediaItemRow>(@"
                select id, mediaTypeId as mediaType
                from MediaItemIds
                where id in @mediaItemIds
            ", new
            {
                mediaItemIds = ids
            });

            var results = new List<IHasId>();

            //get all movies
            var movieIds = mediaTypes.Where(x => x.MediaType == MediaType.MOVIE).Select(x => x.Id);
            //TODO - update to filter by only queried column names
            var movies = await this.MovieRepository.GetByIds(movieIds, this.MovieRepository.AllColumnNames);
            results.AddRange(movies);

            //return the results in the order of the ids specified
            var orderedResults = results.OrderBy(x => ids.IndexOf(x.Id));

            return orderedResults;
        }

        /// <summary>
        /// Get a list of search results from all media types
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public async Task<IEnumerable<object>> GetSearchResults(string searchText)
        {
            var searchResults = this.SearchCatalog.GetSearchResults(searchText);
            var ids = searchResults.Select(x => x.MediaItemId);
            var items = await this.GetByIds(ids);
            return items;
        }
    }

    public class MediaItemArguments
    {
        public IEnumerable<int> MediaItemIds { get; set; }
        public string SearchText { get; set; }
    }
}