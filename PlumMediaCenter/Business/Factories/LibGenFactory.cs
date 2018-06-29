using System;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;

namespace PlumMediaCenter.Business.Factories
{
    public class LibGenFactory
    {
        public LibGenFactory(
            Lazy<LibGenMovieRepository> lazyLibGenMovieRepository,
            Lazy<MovieMetadataProcessor> lazyMovieMetadataProcessor,
            AppSettings appSettings,
            Utility utility,
            Lazy<LibGenTvShowRepository> lazyLibGenTvShowRepository,
            Lazy<TvShowMetadataProcessor> lazyTvShowMetadataProcessor,
            SourceRepository sourceRepository
        )
        {
            this.LazyLibGenMovieRepository = lazyLibGenMovieRepository;
            this.LazyMovieMetadataProcessor = lazyMovieMetadataProcessor;
            this.AppSettings = appSettings;
            this.Utility = utility;
            this.LazyLibGenTvShowRepository = lazyLibGenTvShowRepository;
            this.LazyTvShowMetadataProcessor = lazyTvShowMetadataProcessor;
            this.SourceRepository = sourceRepository;

        }
        Lazy<LibGenMovieRepository> LazyLibGenMovieRepository;
        SourceRepository SourceRepository;


        Lazy<TvShowMetadataProcessor> LazyTvShowMetadataProcessor;

        TvShowMetadataProcessor TvShowMetadataProcessor
        {
            get
            {
                return this.LazyTvShowMetadataProcessor.Value;
            }
        }

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

        Lazy<LibGenTvShowRepository> LazyLibGenTvShowRepository;
        LibGenTvShowRepository LibGenTvShowRepository
        {
            get
            {
                return LazyLibGenTvShowRepository.Value;
            }
        }


        AppSettings AppSettings;
        Utility Utility;
        public LibGenMovie BuildMovie(string folderPath, int sourceId)
        {
            return new LibGenMovie(
                folderPath,
                sourceId,
                LibGenMovieRepository,
                MovieMetadataProcessor,
                AppSettings,
                Utility,
                SourceRepository
            );
        }

        public LibGenTvShow BuildTvShow(string folderPath, int sourceId)
        {
            return new LibGenTvShow(
                folderPath,
                sourceId,
                LibGenTvShowRepository,
                AppSettings,
                Utility,
                TvShowMetadataProcessor
            );
        }
    }
}