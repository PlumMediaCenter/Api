using System;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Models
{
    public class MediaHistoryRecord : MediaItemProgress
    {
        public MediaType MediaType;
        public string PosterUrl;
        public string Title;
        public int RuntimeSeconds;

        /// <summary>
        /// The number of total seconds of progress. For example, if a movie was watched from 30 seconds to 50 seconds, this would equal 20 seconds.
        /// </summary>
        /// <returns></returns>
        public int? TotalProgressSeconds
        {
            get
            {
                return this.ProgressSecondsEnd - this.ProgressSecondsBegin;
            }
        }

    }
}