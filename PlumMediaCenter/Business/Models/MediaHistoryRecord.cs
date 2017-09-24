using System;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Models
{
    public class MediaHistoryRecord : MediaProgress
    {
        public MediaTypeId MediaTypeId;
        public string PosterUrl;
        public string Title;
        public int RuntimeMinutes;
        public new int? ProgressSecondsBegin
        {
            private get
            {
                return this.ProgressMinutesBegin * 60;
            }
            set
            {
                this.ProgressMinutesBegin = value / 60;
            }
        }
        public int? ProgressMinutesBegin { get; set; }

        public new int? ProgressSecondsEnd
        {
            private get
            {
                return this.ProgressMinutesEnd * 60;
            }
            set
            {
                this.ProgressMinutesEnd = value / 60;
            }
        }
        public int? ProgressMinutesEnd { get; set; }

        /// <summary>
        /// The number of total minutes of progress. For example, if a movie was watched from 4 minutes to 6 minutes, this would equal 2 (6-4).
        /// </summary>
        /// <returns></returns>
        public int? TotalProgressMinutes
        {
            get
            {
                return this.ProgressMinutesEnd - this.ProgressMinutesBegin;
            }
        }

    }
}