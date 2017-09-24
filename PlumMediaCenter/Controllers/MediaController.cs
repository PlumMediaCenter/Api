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
    public class MediaController : BaseController
    {

        [HttpPost("progress")]
        public async Task<MediaProgress> SetProgress([FromBody]Progress progress)
        {
            if (progress.mediaItemId == null)
            {
                throw new Exception("Media ID is required");
            }
            if (progress.seconds == null)
            {
                throw new Exception("Media ID is required");
            }
            return await this.Manager.Media.SetProgress(this.Manager.Users.CurrentProfileId, progress.mediaItemId.Value, (int)progress.seconds);
        }

        [HttpGet("mediaTypes")]
        public async Task<IEnumerable<MediaTypeObj>> GetAll()
        {
            return await this.Manager.Media.GetAllMediaTypes();
        }

        [HttpGet("progress/{mediaItemId}")]
        public async Task<MediaProgress> GetCurrentProgressForMediaItem(ulong mediaItemId)
        {
            return await this.Manager.Media.GetCurrentProgress(this.Manager.Users.CurrentProfileId, mediaItemId);
        }

        [HttpGet("fakeHistory")]
        public async Task InsertFakeHistory()
        {
            var mediaItemIds = (await this.Manager.Media.QueryAsync<ulong>("select distinct id from MediaItemIds")).ToList();
            var random = new Random();
            var dateBegin = DateTime.UtcNow.AddDays(-500);
            DateTime dateEnd;
            //store many random progress items
            for (var i = 0; i < 200; i++)
            {
                dateBegin = dateBegin.AddDays(random.Next(0, 5)).AddHours(random.Next(1, 24));
                dateEnd = dateBegin.AddDays(random.Next(0, 5)).AddHours(random.Next(1, 24));
                await this.Manager.Media.InsertMediaProgress(new MediaProgress
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

        [HttpGet("history")]
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistory([FromQuery] uint index = 0, [FromQuery] uint limit = 50)
        {
            return await this.Manager.Media.GetHistory(this.Manager.Users.CurrentProfileId, index, limit);
        }

        [HttpGet("history/{mediaItemId}")]
        public async Task<IEnumerable<MediaHistoryRecord>> GetHistoryForMediaItem(uint mediaItemId)
        {
            return await this.Manager.Media.GetHistory(this.Manager.Users.CurrentProfileId, mediaItemId);
        }

        public class Progress
        {
            public ulong? mediaItemId;
            public decimal? seconds;
        }

        [HttpGet("item")]
        public async Task<object> GetMediaItem([FromQuery] uint? mediaItemId)
        {
            return await this.Manager.Media.GetMediaItem(mediaItemId.Value);
        }

        [HttpDelete("history")]
        public async Task DeleteHistoryRecord([FromQuery] ulong? id)
        {
            if (id == null)
            {
                throw new Exception("No id provided");
            }
            await this.Manager.Media.DeleteHistoryRecord(id.Value);
        }

    }
}
