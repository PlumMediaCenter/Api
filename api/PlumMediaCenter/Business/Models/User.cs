using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Models
{
    public class User : IHasId
    {
        public int Id { get; set; }
    }
}