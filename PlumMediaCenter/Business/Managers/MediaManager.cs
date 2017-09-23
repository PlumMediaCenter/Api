using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Data;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Managers
{
    public class MediaManager : BaseManager
    {
        public MediaManager(Manager manager) : base(manager)
        {
        }

        public async Task<MediaProgress> SetProgress(int profileId, ulong mediaId, int progressSeconds)
        {
            //get the last progress record for this user and item
            var progress = (await this.QueryAsync<MediaProgress>(@"
                select * from mediaProgress
                where profileId = @profileId and mediaId = @mediaId
                order by dateEnd desc
                limit 1
            ", new
            {
                profileId = profileId,
                mediaId = mediaId
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
                MediaId = mediaId,
                ProgressSecondsBegin = progressSeconds,
                ProgressSecondsEnd = progressSeconds,
                DateBegin = DateTime.UtcNow,
                DateEnd = DateTime.UtcNow
            });
        }

        public async Task<MediaProgress> InsertMediaProgress(MediaProgress progress)
        {
            progress.Id = (await this.QueryAsync<ulong?>(@"
                insert into mediaProgress(profileId, mediaId, progressSecondsBegin,progressSecondsEnd, dateBegin, dateEnd)
                values(@profileId, @mediaId, @progressSecondsBegin, @progressSecondsEnd, @dateBegin, @dateEnd);
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
                    mediaId=@mediaId, 
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
        public async Task<MediaProgress> GetCurrentProgress(int profileId, ulong mediaId)
        {
            var progress = (await this.QueryAsync<MediaProgress>(@"
                select * from mediaProgress
                where 
                    profileId = @profileId
                    and mediaId = @mediaId
                order by dateEnd desc
                limit 1
            ", new
            {
                profileId = profileId,
                mediaId = mediaId
            })).FirstOrDefault();
            return progress;
        }

        public async Task<IEnumerable<MediaHistoryRecord>> GetHistory(int profileId, uint index = 0, uint limit = 50)
        {
            var items = (await this.QueryAsync<MediaHistoryRecord>(@"
                select * from MediaProgress, MediaIds
                where   
                    MediaProgress.mediaId = MediaIds.id
                    and profileId = @profileId
                order by dateEnd desc
                limit @limit offset @index
            ", new
            {
                profileId = profileId,
                index = index,
                limit = limit
            }));
            //get all of the movies for these media items
            var movieIds = items.Where(x => x.MediaTypeId == MediaTypeId.Movie).Select(x => x.MediaId.Value);
            var movies = await this.Manager.Movies.GetByIds(movieIds);
            foreach (var item in items)
            {
                var movie = movies.Where(x => x.Id == item.MediaId).FirstOrDefault();
                item.PosterUrl = movie.PosterUrl;
                item.RuntimeMinutes = movie.RuntimeMinutes;
                item.Title = movie.Title;
            }
            return items;
        }

        public async Task<ulong> GetNewMediaId(MediaTypeId mediaTypeId)
        {
            using (var connection = GetNewConnection())
            {
                var rows = await this.QueryAsync<ulong?>(@"
                    insert into mediaIds(mediaTypeId) values(@mediaTypeId);select last_insert_id();
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
    }
}