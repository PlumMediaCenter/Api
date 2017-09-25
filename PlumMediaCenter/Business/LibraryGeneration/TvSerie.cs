using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class TvSerie
    {
        public TvSerie(Manager manager, string folderPath, int sourceId)
        {
            this.Manager = manager;
            this.FolderPath = folderPath;
            this.SourceId = sourceId;
        }
        private Manager Manager;

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