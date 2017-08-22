using System.IO;
using PlumMediaCenter.Business;
namespace PlumMediaCenter.Models
{
    public class Movie
    {
        public ulong Id;
        public string Title;
        public ulong SourceId;
        public string Description;
        public string PosterUrl
        {
            get
            {
                return $"{Utility.BaseUrl}posters/{this.Id}.jpg";
            }
        }
        public string BackdropUrl
        {
            get
            {
                return $"{Utility.BaseUrl}backdrops/{this.Id}.jpg";
            }
        }
        public string VideoUrl
        {
            get
            {
                //get just the filename from the videopath
                var filename = Path.GetFileName(VideoPath);


                return $"{Business.Utility.BaseUrl}source{this.SourceId}/{this.FolderName}/{filename}";
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
            private get
            {
                return this._VideoPath;
            }
        }

        private string _FolderPath;
        public string FolderPath
        {
            set
            {
                this._FolderPath = value;
            }
            private get
            {
                return this._FolderPath;
            }
        }

        private string FolderName
        {
            get
            {
                var info = new DirectoryInfo(this.VideoPath);
                return info.Parent.Name;
            }
        }

    }
}