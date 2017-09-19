namespace PlumMediaCenter.Data
{
    public class Source
    {
        public ulong? Id;
        public string FolderPath;
        /// <summary>
        /// The type of media this source contains (movies, series)
        /// </summary>
        public MediaType MediaType;

    }

    public enum MediaType
    {
        Movie = 1,
        TvShow = 2,
        TvEpisode = 3
    }

}