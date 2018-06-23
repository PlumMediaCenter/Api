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
            Utility Utility,
            Lazy<LibGenTvShowRepository> lazyLibGenTvShowRepository,
            Lazy<TvShowMetadataProcessor> LazyTvShowMetadataProcessor
        )
        {
            this.LazyLibGenMovieRepository = LazyLibGenMovieRepository;
            this.LazyMovieMetadataProcessor = LazyMovieMetadataProcessor;
            this.AppSettings = AppSettings;
            this.Utility = Utility;
            this.LazyLibGenTvShowRepository = lazyLibGenTvShowRepository;
            this.LazyTvShowMetadataProcessor = LazyTvShowMetadataProcessor;
        }
        Lazy<LibGenMovieRepository> LazyLibGenMovieRepository;

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
                Utility
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