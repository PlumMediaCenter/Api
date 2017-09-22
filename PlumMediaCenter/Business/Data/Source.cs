namespace PlumMediaCenter.Data
{
    public class Source
    {
        public ulong? Id;
        public string FolderPath;
        /// <summary>
        /// The type of media this source contains (movies, series)
        /// </summary>
        public MediaTypeId MediaTypeId;

    }

    public enum MediaTypeId
    {
        Movie = 1,
        TvShow = 2,
        TvEpisode = 3
    }

}