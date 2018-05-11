using System;
using System.IO;
using System.Threading.Tasks;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.LibraryGeneration;
using Xunit;

namespace PlumMediaCenter.Tests.Business.LibraryGeneration
{
    public class MovieTests
    {
        private Manager Manager = new Manager("http://www.plummediacenter.com");
        [Fact]
        public void GetYearFromFolderName_Works()
        {
            Assert.Equal(2000, Movie.GetYearFromFolderName("Some movie (2000)"));
            Assert.Equal(null, Movie.GetYearFromFolderName("Some movie (from the future)"));
            Assert.Equal(2002, Movie.GetYearFromFolderName("Some movie (from the future) (2002)"));
            Assert.Equal(null, Movie.GetYearFromFolderName(""));
            Assert.Equal(null, Movie.GetYearFromFolderName(null));
        }

        [Fact]
        public void TitlesAreEquivalent_Works()
        {
            Assert.True(Movie.TitlesAreEquivalent("Self/less", "Self-less"));
            Assert.True(Movie.TitlesAreEquivalent("Self/less", "Self less"));
            Assert.False(Movie.TitlesAreEquivalent("Self/less", "Selfless"));
            Assert.False(Movie.TitlesAreEquivalent("Self/less", "Cat"));
            Assert.True(Movie.TitlesAreEquivalent("Pride & Prejudice", "Pride and Prejudice"));
            Assert.True(Movie.TitlesAreEquivalent("Monumental: In Search of America's National Treasure", "Monumental - In Search of America's National Treasure"));
            
        }

        [Fact]
        public async Task DownloadMetadataIfPossible_Works()
        {
            var path = CreateTestMovie("Hannibal Rising");
            var movie = new Movie(Manager, path, 99999);
            await movie.DownloadMetadataIfPossible();
            var movieJsonPath = Utility.NormalizePath($"{path}/movie.json", true);
            var posterPath = Utility.NormalizePath($"{path}/poster.jpg", true);
            var backdropsPath = Utility.NormalizePath($"{path}/backdrops", false);

            Assert.True(File.Exists(movieJsonPath));
            Assert.True(File.Exists(posterPath));
            Assert.True(Directory.Exists(backdropsPath));
        }

        private string CreateTestMovie(string movieFolderName)
        {
            var path = Utility.NormalizePath($"{Directory.GetCurrentDirectory()}/{movieFolderName}", false);
            Directory.CreateDirectory(path);
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void Process_CreatesMovieDotJsonForNoMetadata()
        {
            //notice dalmatians is spelled wrong (with an o)
            var path = CreateTestMovie("101 Dalmations (1234)");
            var movie = new Movie(Manager, path, 1);
            var metadata = movie.GetGenericMetadata();
            Assert.Equal("101 Dalmations", metadata.Title);
        }
    }
}
