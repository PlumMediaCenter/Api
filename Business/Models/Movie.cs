using System.IO;

namespace PlumMediaCenter.Models
{
    public class Movie
    {
        public long Id;
        public string Title;
        public string Description;
        public string PosterUrl;
        public string BackdropUrl;
        public string VideoUrl
        {
            get
            {
                //get just the filename from the videopath
                var filename = Path.GetFileName(_VideoPath);
                
                
                return $"{Business.Utility.BaseUrl}{filename}";
            }
        }
        public int Duration;

        private string _VideoPath;
        public string VideoPath
        {
            set
            {
                this._VideoPath = value;
            }
        }

    }
}