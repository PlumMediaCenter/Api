using System.Data;
using PlumMediaCenter.Business.Managers;

namespace PlumMediaCenter.Business
{
    public class Manager
    {
        public Manager()
        {
            this.Connection = Data.ConnectionManager.GetConnection();
        }

        public IDbConnection Connection;

        private AppSettings _AppSettings;
        public AppSettings AppSettings
        {
            get
            {
                return this._AppSettings = this._AppSettings != null ? this._AppSettings : new AppSettings();
            }
        }

        private SourceManager _Sources;
        public SourceManager Sources
        {
            get
            {
                return this._Sources = this._Sources != null ? this._Sources : new SourceManager();
            }
        }

        private MovieManager _Movies;
        public MovieManager Movies
        {
            get
            {
                return this._Movies = this._Movies != null ? this._Movies : new MovieManager();
            }
        }

        private ShowManager _Shows;
        public ShowManager Shows
        {
            get
            {
                return this._Shows = this._Shows != null ? this._Shows : new ShowManager();
            }
        }
    }
}