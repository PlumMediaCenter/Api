namespace PlumMediaCenter.Data
{
    public class Source
    {
        public ulong? Id;
        public string FolderPath;
        /// <summary>
        /// The type of media this source contains (movies, series)
        /// </summary>
        public SourceType SourceType;
    }

    public enum SourceType
    {
        Movie = 0,
        Show = 1
    }
}