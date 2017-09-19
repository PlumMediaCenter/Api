using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Managers
{
    public class MediaManager : BaseManager
    {
        public MediaManager(Manager manager) : base(manager)
        {
        }

        public async Task SetProgress(int profileId, int mediaId, int progressSeconds)
        {
            await this.ExecuteAsync(@"
                insert into mediaProgress(profileId, mediaId, progressSeconds)
                values(@profileId, @mediaId, @progressSeconds)
            ", new
            {
                profileId = profileId,
                mediaId = mediaId,
                prorgessSeconds = progressSeconds
            });
        }

        public async Task<ulong> GetNewMediaId(MediaType mediaType)
        {
            using (var connection = GetNewConnection())
            {
                var rows = await this.QueryAsync<ulong?>(@"
                    insert into mediaIds(mediaType) values(@mediaType);select last_insert_id();
                ", new { mediaType = (int)mediaType });
                
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