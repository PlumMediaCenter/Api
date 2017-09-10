using System;
using PlumMediaCenter.Business.LibraryGeneration;
using Xunit;

namespace PlumMediaCenter.Tests.Business.LibraryGeneration
{
    public class MovieTests
    {
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
        }
    }
}
