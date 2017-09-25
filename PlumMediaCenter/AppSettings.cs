using System;
using System.IO;
using PlumMediaCenter.Business;

namespace PlumMediaCenter
{
    public class AppSettings
    {
        public string TmdbCacheDirectoryPath
        {
            get
            {
                return $"{Directory.GetCurrentDirectory()}/temp/tmdb-cache/";
            }
        }
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
                var path = Utility.NormalizePath($"{Directory.GetCurrentDirectory()}/temp/", false);
                //make sure the temp folder path exists
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public string TmdbApiString = "90dbc17887e30eae3095d213fa803190";

        /// <summary>
        /// The total number of seconds of wiggle room between media item progress before it creates a new progress record.
        /// For example, when you are watching a movie, every n seconds a new progress event will be sent. Those are consecutive, and because 
        /// the amount of progress from last entry to this entry equals the difference in thime, that gap would be roughly zero.
        /// Now imagine the user pauses the movie for 5 minutes for a snack break. The next progress gap will be roughly 5 minutes. This is still 
        /// the same viewing session, so we need to account for the max size of a gap before creating a new progress record.
        /// </summary>
        public int MaxMediaProgressGapSeconds = 20;

        /// <summary>
        /// The percentage of a video that must be watched before being consered watched.
        /// This value is used whenever a media item does not explicitly indicate a CompletionSeconds value
        /// </summary>
        public int CompletionPercentage
        {
            get
            {
                return CompletionPercentageStatic;
            }
        }

        /// <summary>
        /// The percentage of a video that must be watched before being consered watched.
        /// This value is used whenever a media item does not explicitly indicate a CompletionSeconds value
        /// </summary>
        public static int CompletionPercentageStatic = 95;

        public string BaseUrl
        {
            get
            {
                return BaseUrlStatic;
            }
        }


        /// <summary>
        /// WARNING: Only use this from a request thread!!
        /// A static accessor for the full base url pointing to the root of this api.
        /// </summary>
        /// <returns></returns>
        public static string BaseUrlStatic
        {
            get
            {
                if (Middleware.RequestMiddleware.CurrentHttpContext == null)
                {
                    throw new Exception("Unable to determine base url because current thread does not have an associated HttpContext");
                }

                var store = Middleware.RequestMiddleware.CurrentHttpContext.Items;
                if (store.ContainsKey("baseUrl") == false)
                {
                    var request = Middleware.RequestMiddleware.CurrentHttpContext.Request;
                    string url;
                    //if there is an original url header (sent from a reverse proxy), use that
                    if (request.Headers.ContainsKey("X-ORIGINAL-URL"))
                    {
                        url = request.Headers["X-ORIGINAL-URL"];
                    }
                    else
                    {
                        url = $"{request.Scheme}://{request.Host}{request.Path}";
                    }

                    //remove anything after and including /api/
                    var baseUrl = url.Substring(0, url.ToLowerInvariant().IndexOf("/api/") + 1);
                    store["baseUrl"] = baseUrl;
                }
                return (string)store["baseUrl"];
            }
        }

    }
}