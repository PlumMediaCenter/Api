using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using Xunit;

namespace PlumMediaCenter.Tests.Business.LibraryGeneration
{
    public class UtilityTests
    {
        public UtilityTests()
        {
            this.Utility = new Utility();
        }
        private Utility Utility;
        [Fact]
        public void GetYearFromFolderName_Works()
        {
            Assert.Equal(2000, this.Utility.GetYearFromFolderName("Some movie (2000)"));
            Assert.Equal(null, this.Utility.GetYearFromFolderName("Some movie (from the future)"));
            Assert.Equal(2002, this.Utility.GetYearFromFolderName("Some movie (from the future) (2002)"));
            Assert.Equal(null, this.Utility.GetYearFromFolderName(""));
            Assert.Equal(null, this.Utility.GetYearFromFolderName(null));
        }

        [Fact]
        public void TitlesAreEquivalent_Works()
        {
            Assert.True(this.Utility.TitlesAreEquivalent("Self/less", "Self-less"));
            Assert.True(this.Utility.TitlesAreEquivalent("Self/less", "Self less"));
            Assert.True(this.Utility.TitlesAreEquivalent("Self/less", "Selfless"));
            Assert.True(this.Utility.TitlesAreEquivalent("A Knight's Tale", "A Knights Tale"));
            Assert.True(this.Utility.TitlesAreEquivalent("Pride & Prejudice", "Pride and Prejudice"));
            Assert.True(this.Utility.TitlesAreEquivalent("Monumental: In Search of America's National Treasure", "Monumental - In Search of America's National Treasure"));

            Assert.False(this.Utility.TitlesAreEquivalent("Self/less", "Cat"));
        }

        [Fact]
        public void GetPosterPathsForImagePaths_Works()
        {
            //images with exact same filename
            Assert.Equal(new[] { "C:/movies/avatar/avatar.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar.jpg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/avatar.jpeg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar.jpeg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/avatar.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar.png" }, ImageType.Poster));

            //proper sort order with numbers in filenames
            Assert.Equal(new[] {
                "C:/movies/avatar/avatar.jpg",
                "C:/movies/avatar/avatar-1.jpg",
                "C:/movies/avatar/avatar-2.jpg",
                "C:/movies/avatar/avatar-11.jpg",
            }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/avatar-1.jpg",
                "C:/movies/avatar/avatar-2.jpg",
                "C:/movies/avatar/avatar-11.jpg",
                "C:/movies/avatar/avatar.jpg"
            }, ImageType.Poster));

            //excludes images that don't match the file name
            Assert.Equal(new string[] { }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/cat-1.jpg",
                "C:/movies/avatar/dog-2.jpg",
                "C:/movies/avatar/mouse.jpg"
            }, ImageType.Poster));

            //excludes images look right in the middle but not the end
            Assert.Equal(new string[] { }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/cat-1.jpg123",
                "C:/movies/avatar/dog-2.jpg.something",
                "C:/movies/avatar/mouse.jpg.extra.jpg"
            }, ImageType.Poster));

            //excludes images look right in the middle but not the end
            Assert.Equal(new string[] { }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/cat-1.jpg123",
                "C:/movies/avatar/dog-2.jpg.something",
                "C:/movies/avatar/mouse.jpg.extra.jpg"
            }, ImageType.Poster));

            //handles case sensitive file systems
            PlumMediaCenter.Business.Utility._FileSystemIsCaseSensitive = true;
            Assert.Equal(new string[] {
                "C:/movies/avatar/avatar.jpg"
             }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/avatar.jpg",
                "C:/movies/avatar/avatar.JPG"
            }, ImageType.Poster));
            PlumMediaCenter.Business.Utility._FileSystemIsCaseSensitive = null;

            //supports multiple "default" poster names.
            Assert.Equal(new[] { "C:/movies/avatar/cover.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/cover.jpg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/default.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/default.jpg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/folder.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/folder.jpg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/movie.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/movie.jpg" }, ImageType.Poster));
            Assert.Equal(new[] { "C:/movies/avatar/poster.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/poster.jpg" }, ImageType.Poster));
        }

        [Fact]
        public void GetBackdropPathsForImagePaths_Works()
        {
            //images with exact same filename
            Assert.Equal(new[] { "C:/movies/avatar/avatar-backdrop.jpg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-backdrop.jpg" }, ImageType.Backdrop));
            Assert.Equal(new[] { "C:/movies/avatar/avatar-backdrop.jpeg" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-backdrop.jpeg" }, ImageType.Backdrop));
            Assert.Equal(new[] { "C:/movies/avatar/avatar-backdrop.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-backdrop.png" }, ImageType.Backdrop));

            //finds the various forms of default backdrop name
            Assert.Equal(new[] { "C:/movies/avatar/avatar-art.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-art.png" }, ImageType.Backdrop));
            Assert.Equal(new[] { "C:/movies/avatar/avatar-backdrop.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-backdrop.png" }, ImageType.Backdrop));
            Assert.Equal(new[] { "C:/movies/avatar/avatar-background.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-background.png" }, ImageType.Backdrop));
            Assert.Equal(new[] { "C:/movies/avatar/avatar-fanart.png" }, this.Utility.FilterAndSortImagePaths("avatar", new[] { "C:/movies/avatar/avatar-fanart.png" }, ImageType.Backdrop));

            //proper sort order with numbers in filenames
            Assert.Equal(new[] {
                "C:/movies/avatar/avatar-fanart.png" ,
                "C:/movies/avatar/avatar-fanart-1.png" ,
                "C:/movies/avatar/avatar-fanart-2.png" ,
                "C:/movies/avatar/avatar-fanart-3.png",
                "C:/movies/avatar/avatar-fanart-11.png"
            }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/avatar-fanart-3.png" ,
                "C:/movies/avatar/avatar-fanart-1.png" ,
                "C:/movies/avatar/avatar-fanart-2.png" ,
                "C:/movies/avatar/avatar-fanart-11.png" ,
                "C:/movies/avatar/avatar-fanart.png"
            }, ImageType.Backdrop));

            //excludes images that don't match the file name
            Assert.Equal(new string[] { }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/cat-1.jpg",
                "C:/movies/avatar/dog-2.jpg",
                "C:/movies/avatar/mouse.jpg"
            }, ImageType.Backdrop));

            //excludes images look right in the middle but not the end
            Assert.Equal(new string[] { }, this.Utility.FilterAndSortImagePaths("avatar", new[] {
                "C:/movies/avatar/avatar-backdrop-1.jpg123",
                "C:/movies/avatar/avatar-backdrop-2.jpg.something",
                "C:/movies/avatar/avatar-backdrop.jpg.extra.jpg"
            }, ImageType.Poster));

        }
    }
}
