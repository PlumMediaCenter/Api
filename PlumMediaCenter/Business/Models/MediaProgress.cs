using System;

namespace PlumMediaCenter.Models
{
    public class MediaProgress
    {
        public ulong? Id { get; set; }
        public int? ProfileId { get; set; }
        public ulong? MediaItemId { get; set; }
        public int? ProgressSecondsBegin { get; set; }
        public int? ProgressSecondsEnd { get; set; }
        public DateTime DateBegin { get; set; }
        public DateTime DateEnd { get; set; }
    }
}