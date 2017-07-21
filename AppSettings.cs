using System.IO;

namespace PlumMediaCenter
{
    public class AppSettings
    {
        public string PosterFolderPath
        {
            get
            {
                return $"wwwroot{Path.DirectorySeparatorChar}posters";
            }
        }
    }
}