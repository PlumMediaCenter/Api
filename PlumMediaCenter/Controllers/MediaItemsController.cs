using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using PlumMediaCenter.Data;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using PlumMediaCenter.Business;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class MediaItemsController : BaseController
    {

        /// <summary>
        /// Get a subset of history records for all media items.
        /// Only results for current profile are retrieved.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [HttpGet("history")]
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistory([FromQuery] int limit = 50, [FromQuery] int index = 0)
        {
            return await this.Manager.Media.GetHistory(this.CurrentProfileId, limit, index);
        }

        /// <summary>
        /// Get history records for a particular media item (i.e. movie or tv episode).
        /// Only results for current profile are retrieved.
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        [HttpGet("{mediaItemId}/history")]
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryForMediaItem(int mediaItemId, [FromQuery] int limit = 50, [FromQuery] int index = 0)
        {
            return await this.Manager.Media.GetHistoryForMediaItem(this.CurrentProfileId, mediaItemId, limit, index);
        }

        /// <summary>
        /// Delete a history record by its id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("history/{id}")]
        public async Task DeleteHistoryRecord(int id)
        {
            await this.Manager.Media.DeleteHistoryRecord(id);
        }

        /// <summary>
        /// Set the progress for a media item. Conventionally this is called by the video player several times a minute to update
        /// the latest position (in seconds) of the video
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        [HttpPost("{mediaItemId}/progress")]
        public async Task<MediaItemProgress> SetProgress(int mediaItemId, [FromBody]Progress progress)
        {
            if (progress == null || progress.seconds == null)
            {
                throw new Exception("Media ID is required");
            }
            return await this.Manager.Media.SetProgress(this.CurrentProfileId, mediaItemId, (int)progress.seconds);
        }

        public class Progress
        {
            public decimal? seconds;
        }

        [HttpGet("fakeHistory")]
        public async Task InsertFakeHistory()
        {
            var mediaItemIds = (await this.Manager.Media.QueryAsync<int>("select distinct id from MediaItemIds")).ToList();
            var random = new Random();
            var dateBegin = DateTime.UtcNow.AddDays(-500);
            DateTime dateEnd;
            //store many random progress items
            for (var i = 0; i < 200; i++)
            {
                dateBegin = dateBegin.AddDays(random.Next(0, 5)).AddHours(random.Next(1, 24));
                dateEnd = dateBegin.AddDays(random.Next(0, 5)).AddHours(random.Next(1, 24));
                await this.Manager.Media.InsertMediaProgress(new MediaItemProgress
                {
                    ProfileId = 1,
                    MediaItemId = mediaItemIds[random.Next(0, mediaItemIds.Count - 1)],
                    DateBegin = dateBegin,
                    DateEnd = dateEnd,
                    ProgressSecondsBegin = random.Next(0, 100),
                    ProgressSecondsEnd = random.Next(101, 367)
                });
            }
        }

        /// <summary>
        /// Get a media item by its id. 
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        [HttpGet("{mediaItemId}")]
        public async Task<object> GetMediaItemById(int mediaItemId)
        {
            return await this.Manager.Media.GetMediaItem(mediaItemId);
        }

        /// <summary>
        /// Get the number of seconds at which the media item should resume.
        /// If media item has never been interacted, this will return 0.
        /// </summary>
        /// <param name="mediaItemId"></param>
        /// <returns></returns>
        [HttpGet("{mediaItemId}/resumeSeconds")]
        public async Task<object> GetResumeSeconds(int mediaItemId)
        {
            return await this.Manager.Media.GetMediaItemResumeSeconds(this.CurrentProfileId, mediaItemId);
        }

        [HttpGet]
        public async Task<object> Search([FromQuery]string q)
        {
            return await this.Manager.Media.GetSearchResults(q);
        }
    }
}
