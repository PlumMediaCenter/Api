using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Models
{
    public class LibGenTvSerie
    {
        public LibGenTvSerie(string folderPath, int sourceId)
        {
            this.FolderPath = folderPath;
            this.SourceId = sourceId;
        }

        /// <summary>
        /// A full path to the root folder of this tv serie
        /// </summary>
        private string FolderPath;

        /// <summary>
        /// The id for the video source
        /// </summary>
        public int SourceId;

        /// <summary>
        /// 
        /// </summary>
        public void Process()
        {

        }
    }
}