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
                return $"{Directory.GetCurrentDirectory()}/wwwroot/posters/";
            }
        }

        public string BackdropFolderPath
        {
            get
            {
                return $"{Directory.GetCurrentDirectory()}/wwwroot/backdrops/";
            }
        }

        public static string TempPath
        {
            get
            {
                return $"{Directory.GetCurrentDirectory()}/temp";
            }
        }
        public string TmdbApiString
        {
            get
            {
                return "90dbc17887e30eae3095d213fa803190";
            }
        }
    }
}