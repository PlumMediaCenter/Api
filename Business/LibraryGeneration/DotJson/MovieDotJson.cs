using System.Collections.Generic;

namespace PlumMediaCenter.Business.LibraryGeneration.DotJson
{
    public class MovieDotJson
    {
        /// <summary>
        /// The title of the movie
        /// </summary>
        public string Title;
        /// <summary>
        /// A collection of all possible titles for the movie. This will also include the Title property
        /// </summary>
        public List<string> Titles;
        /// <summary>
        /// A short summary of the movie
        /// </summary>
        public string Summary;
        /// <summary>
        /// A lengthier description of the movie
        /// </summary>
        public string Description;

        /// <summary>
        /// The name of the collection that this video belongs to (i.e. 'Star Trek', 'Die Hard').
        /// If the movie is not in a collection, this will be null
        /// </summary>
        public string Collection;
        /// <summary>
        /// The number in the collection, if it is in one. (i.e. Star Trek: The Wrath of Kahn would be 2, since it's the second star trek movie)
        /// </summary>
        public int? CollectionOrder;
        /// <summary>
        /// A list of all actors in the film
        /// </summary>
        public List<CastMember> Cast = new List<CastMember>();
        /// <summary>
        /// A list of crew members who worked on the film (directors, writers)
        /// </summary>
        /// <returns></returns>
        public List<CrewMember> Crew = new List<CrewMember>();
        /// <summary>
        /// A list of high-level genres for the movie. These would be things like "Action", "Thriller"
        /// </summary>
        public List<string> Genres;
        /// <summary>
        /// A fine-grained list of things related to the movie. Kind of like sub-genres
        /// </summary>
        public List<string> Keywords = new List<string>();
        /// <summary>
        /// The MPAA rating (G, PG, R, etc...)
        /// </summary>
        public string Rating;

        /// <summary>
        /// The date this movie was released
        /// </summary>
        public System.DateTime ReleaseDate;
        
        /// <summary>
        /// The runtime of the movie in minutes
        /// </summary>
        public int? Runtime;
        /// <summary>
        /// The TMDB ID for this movie. Null if movie is not on TMDB
        /// </summary>
        public int? TmdbId;
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