using System;
using System.Data;
using PlumMediaCenter.Business.Managers;
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


        public AppSettings AppSettings
        {
            get
            {
                return this._AppSettings = this._AppSettings ?? new AppSettings();
            }
        }
        private AppSettings _AppSettings;

        public Managers.MovieManager Movies
        {
            get
            {
                return this._Movies = this._Movies ?? new Managers.MovieManager(this);
            }
        }
        private Managers.MovieManager _Movies;

        public Managers.MediaManager Media
        {
            get
            {
                return this._Media = this._Media ?? new Managers.MediaManager(this);
            }
        }
        private Managers.MediaManager _Media;

        public MovieMetadataProcessor MovieMetadataProcessor
        {
            get
            {
                return this._MovieMetadataProcessor = _MovieMetadataProcessor ?? new MovieMetadataProcessor(this);
            }
        }
        public MovieMetadataProcessor _MovieMetadataProcessor;

        public UserManager Users
        {
            get
            {
                return this._Users = this._Users ?? new UserManager(this);
            }
        }
        private UserManager _Users;

        public Utility Utility
        {
            get
            {
                return this._Utility = this._Utility ?? new Utility();
            }
        }
        private Utility _Utility;
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
                return this._Sources = this._Sources ?? new LibraryGeneration.Managers.SourceManager(this.Manager);
            }
        }

        private LibraryGeneration.Managers.MovieManager _Movies;
        public LibraryGeneration.Managers.MovieManager Movies
        {
            get
            {
                return this._Movies = this._Movies ?? new LibraryGeneration.Managers.MovieManager(this.Manager);
            }
        }

        private LibraryGeneration.Managers.TvSerieManager _Shows;
        public LibraryGeneration.Managers.TvSerieManager TvSeries
        {
            get
            {
                return this._Shows = this._Shows ?? new LibraryGeneration.Managers.TvSerieManager(this.Manager);
            }
        }
    }
}