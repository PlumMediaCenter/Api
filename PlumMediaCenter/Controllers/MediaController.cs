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

namespace PlumMediaCenter.Controllers
{
    [Route("api/[controller]")]
    [ExceptionHandlerFilter]
    public class MediaController : BaseController
    {

        [HttpGet("progress")]
        public async Task SetProgress([FromQuery]int mediaId, int seconds)
        {
            await this.Manager.Media.SetProgress(this.Manager.Users.CurrentProfileId, mediaId, seconds);
        }

        [HttpGet("mediaTypes")]
        public async Task<IEnumerable<MediaTypeObj>> GetAll()
        {
            return await this.Manager.Media.GetAllMediaTypes();
        }

    }
}
