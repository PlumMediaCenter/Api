using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using System.Linq;
using System.Collections.Generic;

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
            var moviePaths = new List<string>();

            var movieSources = await this.Manager.Sources.GetByType(SourceType.Movie);
            //find all movie folders from each source
            foreach (var source in movieSources)
            {
                moviePaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all movies from the db
            moviePaths.AddRange(await this.Manager.Movies.GetDirectories());

            //remove any duplicates
            moviePaths = moviePaths.Distinct().ToList();

            //process each movie. movie.Process will handle adding, updating, and deleting
            Parallel.ForEach(moviePaths, async (moviePath) =>
            {
                var movie = new Movie(new Manager(), moviePath);
                await movie.Process();
            });
        }

        private async Task ProcessShows()
        {
            var showPaths = new List<string>();

            var showSources = await this.Manager.Sources.GetByType(SourceType.Show);
            //find all show folders from each source
            foreach (var source in showSources)
            {
                showPaths.AddRange(Directory.GetDirectories(source.FolderPath).ToList());
            }

            //find all shows from the db
            showPaths.AddRange(await this.Manager.Shows.GetDirectories());

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
}