namespace PlumMediaCenter.Business.Metadata
{
    public class CrewMember
    {
        /// <summary>
        /// The TMDB ID for this person
        /// </summary>
        public int? TmdbId;
        /// <summary>
        /// The job that this person had for this tv show (i.e. director, writer)
        /// </summary>
        public string Job;
        /// <summary>
        /// The name of the person
        /// </summary>
        public string Name;
    }
}