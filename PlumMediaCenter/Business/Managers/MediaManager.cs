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

        public async Task<MediaProgress> SetProgress(int profileId, ulong mediaItemId, int progressSeconds)
        {
            //get the last progress record for this user and item
            var progress = (await this.QueryAsync<MediaProgress>(@"
                select * from mediaProgress
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
            return await this.InsertMediaProgress(new MediaProgress
            {
                ProfileId = profileId,
                MediaItemId = mediaItemId,
                ProgressSecondsBegin = progressSeconds,
                ProgressSecondsEnd = progressSeconds,
                DateBegin = DateTime.UtcNow,
                DateEnd = DateTime.UtcNow
            });
        }

        public async Task<MediaProgress> InsertMediaProgress(MediaProgress progress)
        {
            progress.Id = (await this.QueryAsync<ulong?>(@"
                insert into mediaProgress(profileId, mediaItemId, progressSecondsBegin,progressSecondsEnd, dateBegin, dateEnd)
                values(@profileId, @mediaItemId, @progressSecondsBegin, @progressSecondsEnd, @dateBegin, @dateEnd);
                select last_insert_id();
            ", progress)).FirstOrDefault();
            return progress;
        }

        public async Task ReplaceMediaProgress(MediaProgress progress)
        {
            await this.ExecuteAsync(@"
                update mediaProgress
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
        /// Get the latest progress entry for a specific media item
        /// </summary>
        /// <param name="profileId"></param>
        /// <returns></returns>
        public async Task<MediaProgress> GetCurrentProgress(int profileId, ulong mediaItemId)
        {
            var progress = (await this.QueryAsync<MediaProgress>(@"
                select * from mediaProgress
                where 
                    profileId = @profileId
                    and mediaItemId = @mediaItemId
                order by dateEnd desc
                limit 1
            ", new
            {
                profileId = profileId,
                mediaItemId = mediaItemId
            })).FirstOrDefault();
            return progress;
        }

        /// <summary>
        /// Get a full list of all history for the specified media item
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryForMediaItem(int profileId, ulong mediaItemId)
        {
            var ids = (await this.QueryAsync<ulong>(@"
                select id from MediaProgress
                where profileId = @profileId
                  and mediaItemId = @mediaItemId
            ", new
            {
                mediaItemId = mediaItemId
            }));
            return await this.GetHistoryByIds(ids);
        }

        public async Task<IEnumerable<MediaHistoryRecord>> GetHistory(int profileId, uint index = 0, uint limit = 50)
        {
            var ids = (await this.QueryAsync<ulong>(@"
                select id from MediaProgress
                where profileId = @profileId
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
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryByIds(IEnumerable<ulong> ids)
        {
            if (ids.Count() == 0)
            {
                return new List<MediaHistoryRecord>();
            }
            var items = (await this.QueryAsync<MediaHistoryRecord>(@"
                select *, MediaProgress.id as id from MediaProgress, MediaItemIds
                where MediaProgress.mediaItemId = MediaItemIds.id
                  and MediaProgress.id in @ids
                order by dateEnd desc
            ", new
            {
                ids = ids
            }));
            //get all of the movies for these media items
            var movieIds = items.Where(x => x.MediaTypeId == MediaTypeId.Movie).Select(x => x.MediaItemId.Value);
            var movies = await this.Manager.Movies.GetByIds(movieIds);
            foreach (var item in items)
            {
                var movie = movies.Where(x => x.Id == item.MediaItemId).FirstOrDefault();
                item.PosterUrl = movie.PosterUrl;
                item.RuntimeMinutes = movie.RuntimeMinutes;
                item.Title = movie.Title;
            }
            return items;
        }

        public async Task DeleteHistoryRecord(ulong id)
        {
            await this.ExecuteAsync(@"
                delete from MediaProgress
                where id = @id
            ", new { id = id });
        }

        public async Task<ulong> GetNewMediaId(MediaTypeId mediaTypeId)
        {
            using (var connection = GetNewConnection())
            {
                var rows = await this.QueryAsync<ulong?>(@"
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
                select * from mediaTypes
            ");
        }

        /// <summary>
        /// Get the specific model by its media id
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        public async Task<object> GetMediaItem(uint mediaItemId)
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
    }
}