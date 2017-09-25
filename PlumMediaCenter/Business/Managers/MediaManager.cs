using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Data;
using PlumMediaCenter.Models;
using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Business.Managers
{
    public class MediaManager : BaseManager
    {
        public MediaManager(Manager manager) : base(manager)
        {
        }

        public async Task<MediaItemProgress> SetProgress(int profileId, int mediaItemId, int progressSeconds)
        {
            //get the last progress record for this user and item
            var progress = (await this.QueryAsync<MediaItemProgress>(@"
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
                if (diffBetweenThem <= this.Manager.AppSettings.MaxMediaProgressGapSeconds)
                {
                    progress.ProgressSecondsEnd = progressSeconds;
                    progress.DateEnd = DateTime.UtcNow;
                    await this.ReplaceMediaProgress(progress);
                    return progress;
                }
                else
                {
                    //the gap is outside of the acceptable limit, then we need to start a new progress
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
            progress.Id = (await this.QueryAsync<int>(@"
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
            await this.ExecuteAsync(@"
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
        /// Get the number of seconds at which a media item should resume.
        /// If this item has never been interacted with, will return 0.
        /// </summary>
        /// <param name="profileId"></param>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<int> GetMediaItemResumeSeconds(int profileId, int mediaItemId)
        {
            var historyRecord = (await this.GetHistoryForMediaItem(profileId, mediaItemId, 1)).FirstOrDefault();
            //if there is no history for this movie, return zero
            if (historyRecord == null)
            {
                return 0;
            }
            //if the progress is within the threshold of "finished the show", then return zero
            else if (await this.SecondCountIsConsideredFinished(mediaItemId, historyRecord.ProgressSecondsEnd))
            {
                return 0;
            }
            //return the progress
            else
            {
                return historyRecord.ProgressSecondsEnd;
            }
        }

        /// <summary>
        /// Determine if the given number of seconds is close enough to the end the media item to consider it finished.
        /// The common use case for this is when a tv episode  gets to the end credits, there may be several minutes remaining in the video.
        /// The next time the user tries to watch the tv show, that episode should be considered 'watched', and the next episode should be picked.
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<bool> SecondCountIsConsideredFinished(int mediaItemId, int secondCount)
        {
            var mediaItem = await this.GetMediaItem(mediaItemId);
            if (mediaItem.GetType() == typeof(Models.Movie))
            {
                var movie = (Models.Movie)mediaItem;
                return secondCount >= movie.CompletionSeconds;
            }
            else
            {
                throw new Exception("Unknown media item type");
            }
        }

        /// <summary>
        /// Get a full list of all history for the specified media item
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryForMediaItem(int profileId, int mediaItemId, int limit = 50, int index = 0)
        {
            var ids = (await this.QueryAsync<int>(@"
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
            var ids = (await this.QueryAsync<int>(@"
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
            var items = (await this.QueryAsync<MediaHistoryRecord>(@"
                select *, MediaItemProgress.id as id from MediaItemProgress, MediaItemIds
                where MediaItemProgress.mediaItemId = MediaItemIds.id
                  and MediaItemProgress.id in @ids
                order by dateEnd desc
            ", new
            {
                ids = ids
            }));
            //get all of the movies for these media items
            var movieIds = items.Where(x => x.MediaTypeId == MediaTypeId.Movie).Select(x => x.MediaItemId);
            var movies = await this.Manager.Movies.GetByIds(movieIds);
            foreach (var item in items)
            {
                var movie = movies.Where(x => x.Id == item.MediaItemId).FirstOrDefault();
                item.PosterUrl = movie.PosterUrl;
                item.RuntimeSeconds = movie.RuntimeSeconds;
                item.Title = movie.Title;
            }
            return items;
        }

        public async Task DeleteHistoryRecord(int id)
        {
            await this.ExecuteAsync(@"
                delete from MediaItemProgress
                where id = @id
            ", new { id = id });
        }

        public async Task<int> GetNewMediaId(MediaTypeId mediaTypeId)
        {
            using (var connection = GetNewConnection())
            {
                var rows = await this.QueryAsync<int?>(@"
                    insert into MediaItemIds(mediaTypeId) values(@mediaTypeId);select last_insert_id();
                ", new { mediaTypeId = (int)mediaTypeId });

                var id = rows.FirstOrDefault();

                if (id == null || id.Value == 0)
                {
                    throw new Exception("id cannot be null");
                }
                return id.Value;
            }
        }

        public async Task<IEnumerable<MediaTypeObj>> GetAllMediaTypes()
        {
            return await this.QueryAsync<MediaTypeObj>(@"
                select * from MediaTypes
            ");
        }

        /// <summary>
        /// Get the specific model by its media id
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<object> GetMediaItem(int mediaItemId)
        {
            //get the media type id from the db.
            var mediaTypeId = (await this.QueryAsync<MediaTypeId>(@"
                select mediaTypeId 
                from MediaItemIds
                where id = @mediaItemId
            ", new
            {
                mediaItemId = mediaItemId
            })).FirstOrDefault();
            switch (mediaTypeId)
            {
                case MediaTypeId.Movie:
                    return await this.Manager.Movies.GetById(mediaItemId);
                default:
                    throw new Exception("Not implemented");
            }
        }

        /// <summary>
        /// Get a list of search results from all media types
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public async Task<IEnumerable<object>> GetSearchResults(string searchText)
        {
            var movieResults = await this.Manager.Movies.GetSearchResults(searchText);
            return movieResults;
        }
    }
}