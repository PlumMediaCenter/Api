using System.IO;

namespace PlumMediaCenter
{
    public class AppSettings
    {
        /// <summary>
        /// The path to the path to the folder where the posters should live. Includes trailing slash
        /// </summary>
        /// <returns></returns>
        public string PosterFolderPath
        {
            get
            {
                var slash = Path.DirectorySeparatorChar;
                return $"{Directory.GetCurrentDirectory()}{slash}wwwroot{slash}posters{slash}";
            }
        }
    }
}