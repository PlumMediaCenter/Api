using System;
using System.Data;
using PlumMediaCenter.Business.MetadataProcessing;

namespace PlumMediaCenter.Business
{
    public class Manager
    {
        public Manager(string baseUrl)
        {
            this.LibraryGeneration = new LibraryGenerationManager(this);
            this.BaseUrl = baseUrl;
        }

        public LibraryGenerationManager LibraryGeneration;

        public string BaseUrl;

        private Utility _Utility;
        public Utility Utility
        {
            get
            {
                return this._Utility = this._Utility != null ? this._Utility : new Utility();
            }
        }

        private AppSettings _AppSettings;
        public AppSettings AppSettings
        {
            get
            {
                return this._AppSettings = this._AppSettings != null ? this._AppSettings : new AppSettings();
            }
        }

        private Managers.MovieManager _Movies;
        public Managers.MovieManager Movies
        {
            get
            {
                return this._Movies = this._Movies != null ? this._Movies : new Managers.MovieManager(this);
            }
        }
        public MovieMetadataProcessor MovieMetadataProcessor
        {
            get
            {
                return this._MovieMetadataProcessor = _MovieMetadataProcessor != null ? this._MovieMetadataProcessor : new MovieMetadataProcessor(this);
            }
        }
        public MovieMetadataProcessor _MovieMetadataProcessor;
    }

    public class LibraryGenerationManager : BaseManager
    {

        public LibraryGenerationManager(Manager manager) : base(manager)
        {

        }

        private LibraryGeneration.Managers.SourceManager _Sources;
        public LibraryGeneration.Managers.SourceManager Sources
        {
            get
            {
                return this._Sources = this._Sources != null ? this._Sources : new LibraryGeneration.Managers.SourceManager(this.Manager);
            }
        }

        private LibraryGeneration.Managers.MovieManager _Movies;
        public LibraryGeneration.Managers.MovieManager Movies
        {
            get
            {
                return this._Movies = this._Movies != null ? this._Movies : new LibraryGeneration.Managers.MovieManager(this.Manager);
            }
        }

        private LibraryGeneration.Managers.TvSerieManager _Shows;
        public LibraryGeneration.Managers.TvSerieManager TvSeries
        {
            get
            {
                return this._Shows = this._Shows != null ? this._Shows : new LibraryGeneration.Managers.TvSerieManager(this.Manager);
            }
        }
    }
}