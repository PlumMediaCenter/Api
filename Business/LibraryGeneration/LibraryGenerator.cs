using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using System.Linq;
using System.Collections.Generic;
using System;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public class LibraryGenerator
    {
        public LibraryGenerator(Manager manager = null)
        {
            this.Manager = manager != null ? manager : new Manager();
        }
        public Manager Manager;

        public async Task Generate()
        {
            var moviesTask = this.ProcessMovies();
            var seriesTask = this.ProcessShows();
            await Task.WhenAll(moviesTask, seriesTask);
            return;
        }

        private async Task ProcessMovies()
        {
            var moviePaths = new List<MoviePath>();

            var movieSources = await this.Manager.LibraryGeneration.Sources.GetByType(SourceType.Movie);
            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                var directories = Directory.GetDirectories(source.FolderPath).ToList();
                foreach (var dir in directories)
                {
                    moviePaths.Add(new MoviePath { Path = dir, Source = source });
                }
            }

            //find all movies from the db
            var dbMovies = await this.Manager.LibraryGeneration.Movies.GetDirectories();
            foreach (var kvp in dbMovies)
            {
                var source = movieSources.Where(x => x.Id == kvp.Key).First();
                foreach (var path in kvp.Value)
                {
                    moviePaths.Add(new MoviePath { Path = path, Source = source });
                }
            }

            var pathLookup = new Dictionary<string, bool>();
            var distinctList = new List<MoviePath>();
            //remove any duplicates
            foreach (var item in moviePaths)
            {
                if (pathLookup.ContainsKey(item.Path) == false)
                {
                    pathLookup.Add(item.Path, true);
                    distinctList.Add(item);
                }
            }
            moviePaths = distinctList;

            //process each movie. movie.Process will handle adding, updating, and deleting
            Parallel.ForEach(moviePaths, async (moviePath) =>
            {
                var movie = new Movie(new Manager(), moviePath.Path, moviePath.Source.Id.Value);
                await movie.Process();
            });
        }

        private async Task ProcessShows()
        {
            var showPaths = new List<string>();

            var showSources = await this.Manager.LibraryGeneration.Sources.GetByType(SourceType.Show);
            //find all show folders from each source
            foreach (var source in showSources)
            {
                showPaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            showPaths.AddRange(await this.Manager.LibraryGeneration.Shows.GetDirectories());

            //remove any duplicates
            showPaths = showPaths.Distinct().ToList();

            //process each movie. movie.Process will handle adding, updating, and deleting
            Parallel.ForEach(showPaths, moviePath =>
            {
                var show = new Show(this.Manager, moviePath);
                show.Process();
            });
        }

    }
    class MoviePath
    {
        public string Path;
        public Source Source;
    }
}