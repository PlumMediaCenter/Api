using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class Show
    {
        public Show(Manager manager, string moviePath)
        {
            this.Manager = manager != null ? manager : new Manager();
            this.MoviePath = moviePath;
        }
        private Manager Manager;

        /// <summary>
        /// A full path to the folder containing this movie
        /// </summary>
        private string MoviePath;

        /// <summary>
        /// 
        /// </summary>
        public void Process()
        {

        }
    }
}