using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Models
{
    public class Source
    {
        public int Id { get; set; }
        public string FolderPath { get; set; }
        //setter for mutations
        public MediaType MediaType { get; set; }
    }

}