using System;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;

namespace PlumMediaCenter.Business.Factories
{
    public class LibGenFactory
    {
        public LibGenFactory(
            Lazy<LibGenMovieRepository> LazyLibGenMovieRepository,
            Lazy<MovieMetadataProcessor> LazyMovieMetadataProcessor,
            AppSettings AppSettings,
            Utility Utility
        )
        {
            this.LazyLibGenMovieRepository = LazyLibGenMovieRepository;
            this.LazyMovieMetadataProcessor = LazyMovieMetadataProcessor;
            this.AppSettings = AppSettings;
            this.Utility = Utility;
        }
        Lazy<LibGenMovieRepository> LazyLibGenMovieRepository;
        LibGenMovieRepository LibGenMovieRepository
        {
            get
            {
                return LazyLibGenMovieRepository.Value;
            }
        }
        Lazy<MovieMetadataProcessor> LazyMovieMetadataProcessor;
        MovieMetadataProcessor MovieMetadataProcessor
        {
            get
            {
                return LazyMovieMetadataProcessor.Value;
            }
        }
        AppSettings AppSettings;
        Utility Utility;
        public LibGenMovie BuildMovie(string folderPath, int sourceId)
        {
            return new LibGenMovie(
                LibGenMovieRepository,
                MovieMetadataProcessor,
                AppSettings,
                Utility,
                folderPath,
                sourceId
            );
        }

        public LibGenTvSerie BuildTvSerie(string folderPath, int sourceId)
        {
            return new LibGenTvSerie(folderPath, sourceId);
        }
    }
}