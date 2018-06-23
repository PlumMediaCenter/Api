using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Data;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Models
{
    public class MediaItem : IHasId
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public MediaType MediaType { get; set; }
    }
}