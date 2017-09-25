using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Data
{
    public class Source
    {
        public int? Id;
        public string FolderPath;
        /// <summary>
        /// The type of media this source contains (movies, series)
        /// </summary>
        public MediaTypeId MediaTypeId;

    }

}