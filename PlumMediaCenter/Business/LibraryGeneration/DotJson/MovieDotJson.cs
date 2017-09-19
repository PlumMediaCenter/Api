using System.Collections.Generic;

namespace PlumMediaCenter.Business.LibraryGeneration.DotJson
{
    public class MovieDotJson
    {
        public MovieDotJson()
        {

        }
        /// <summary>
        /// Create a new object based on an existing object. This is only a shallow clone, but only currently exists so that
        /// we can write the file to disc without serializing additional properties from child objects
        /// </summary>
        /// <param name="movieDotJson"></param>
        public MovieDotJson(MovieDotJson movieDotJson)
        {
            var t = this.GetType();
            var properties = t.GetProperties();
            foreach (var prop in properties)
            {
                var incomingValue = prop.GetValue(movieDotJson);
                prop.SetValue(this, incomingValue);
            }
        }
        /// <summary>
        /// The title of the movie
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The title to use to sort the movie with. If omitted, Title should be used
        /// </summary>
        /// <returns></returns>
        public string SortTitle { get; set; }
        /// <summary>
        /// A collection of all possible titles for the movie. This will also include the Title property
        /// </summary>
        public List<string> SearchText { get; set; } = new List<string>();
        /// <summary>
        /// A short summary of the movie
        /// </summary>
        public string Summary { get; set; }
        /// <summary>
        /// A lengthier description of the movie
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the collection that this video belongs to (i.e. 'Star Trek', 'Die Hard').
        /// If the movie is not in a collection, this will be null
        /// </summary>
        public string Collection { get; set; }
        /// <summary>
        /// The number in the collection, if it is in one. (i.e. Star Trek: The Wrath of Kahn would be 2, since it's the second star trek movie)
        /// </summary>
        public int? CollectionOrder { get; set; }
        /// <summary>
        /// A list of all actors in the film
        /// </summary>
        public List<CastMember> Cast { get; set; } = new List<CastMember>();
        /// <summary>
        /// A list of crew members who worked on the film (directors, writers)
        /// </summary>
        /// <returns></returns>
        public List<CrewMember> Crew { get; set; } = new List<CrewMember>();
        /// <summary>
        /// A list of high-level genres for the movie. These would be things like "Action", "Thriller"
        /// </summary>
        public List<string> Genres { get; set; } = new List<string>();
        /// <summary>
        /// A fine-grained list of things related to the movie. Kind of like sub-genres
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();
        /// <summary>
        /// The MPAA rating (G, PG, R, etc...)
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// The date this movie was released
        /// </summary>
        public System.DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// The runtime of the movie in minutes
        /// </summary>
        public int? Runtime { get; set; }
        /// <summary>
        /// The TMDB ID for this movie. Null if movie is not on TMDB
        /// </summary>
        public int? TmdbId { get; set; }

        /// <summary>
        /// A collection of posters. Each item is relative to the root folder, and should use linux slashes
        /// </summary>
        /// <returns></returns>
        public List<Image> Backdrops { get; set; } = new List<Image>();
    }

    public class Image
    {
        /// <summary>
        /// The relative path to the image, relative to the root movie folder
        /// </summary>
        public string Path;
        /// <summary>
        /// The URL used to originally download the image. 
        /// </summary>
        public string SourceUrl;
    }

    public class CastMember
    {
        /// <summary>
        /// The TMDB ID for this person
        /// </summary>
        public int? TmdbId;
        /// <summary>
        /// The full name of the character that this actor portrayed
        /// </summary>
        public string Character;
        /// <summary>
        /// The full name of the actor
        /// </summary>
        public string Name;
    }

    public class CrewMember
    {
        /// <summary>
        /// The TMDB ID for this person
        /// </summary>
        public int? TmdbId;
        /// <summary>
        /// The job that this person had for this movie (i.e. director, writer)
        /// </summary>
        public string Job;
        /// <summary>
        /// The name of the person
        /// </summary>
        public string Name;
    }
}