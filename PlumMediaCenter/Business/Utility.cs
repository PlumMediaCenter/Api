using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageSharp;
using SixLabors.Fonts;
using SixLabors.Primitives;

namespace PlumMediaCenter.Business
{
    public class Utility
    {
        private FontFamily _FontFamily;
        private FontFamily FontFamily
        {
            get
            {
                if (_FontFamily == null)
                {
                    var collection = new FontCollection();
                    _FontFamily = collection.Install($"{Directory.GetCurrentDirectory()}/misc/DejaVuSansMono.ttf");
                }
                return _FontFamily;
            }
        }

        /// <summary>
        /// Resize an image to the given width. The height is auto-calculated and will preserve the image's aspect ratio
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="targetWidth"></param>
        public void ResizeImage(string sourcePath, string destinationPath, int targetWidth)
        {
            Image<Rgba32> image;
            using (var file = File.OpenRead(sourcePath)) { image = Image.Load(file); }
            //calculate the new height
            var targetHeight = (int)Math.Ceiling(((float)image.Height / (float)image.Width) * (float)targetWidth);
            using (Stream fileStream = File.Create(destinationPath))
            {
                image.Resize(targetWidth, targetHeight).SaveAsJpeg(fileStream);
            }
        }

        public void CreateTextPoster(string text, string destinationPath)
        {
            Font font = new Font(this.FontFamily, 120f, FontStyle.Regular);
            //Image<Rgba32> image = new Image<Rgba32>(1000, 1500);
            Image<Rgba32> image;

            using (var file = File.OpenRead("misc/BlankPoster.jpg")) { image = Image.Load(file); }

            // image.BackgroundColor(Rgba32.White);

            var maxCharsPerRow = 12;
            float startingCenterY;
            var charWidth = 65;
            var paddingLeft = 60;

            //split the text into rows of 14 characters
            var rows = WordSplit(text, maxCharsPerRow);

            var half = (int)Math.Floor((double)rows.Count / (double)2);
            startingCenterY = 700 - (110 * half);

            foreach (var row in rows)
            {
                //center the text
                var excess = (maxCharsPerRow - row.Length) / (double)2;
                double startingX = paddingLeft + (excess * charWidth);
                var point = new PointF((int)startingX, startingCenterY);
                image.DrawText(row, font, Rgba32.White, point);
                startingCenterY += 110;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            using (Stream fileStream = File.Create(destinationPath))
            {
                image.SaveAsJpeg(fileStream);
            }
        }

        public void CreateTextBackdrop(string text, string destinationPath)
        {
            Font font = new Font(this.FontFamily, 140f, FontStyle.Regular);
            //Image<Rgba32> image = new Image<Rgba32>(1000, 1500);
            Image<Rgba32> image;

            using (var file = File.OpenRead("misc/BlankBackdrop.jpg")) { image = Image.Load(file); }

            // image.BackgroundColor(Rgba32.White);

            var maxCharsPerRow = 22;
            var charWidth = 80;
            var paddingLeft = 60;

            //split the text into rows of 14 characters
            var rows = WordSplit(text, maxCharsPerRow);

            var half = (int)Math.Floor((double)rows.Count / (double)2);
            float startingCenterY = (image.Height / 2) - (120 * half);

            foreach (var row in rows)
            {
                //center the text
                var excess = (maxCharsPerRow - row.Length) / (double)2;
                double startingX = paddingLeft + (excess * charWidth);
                var point = new PointF((int)startingX, startingCenterY);
                image.DrawText(row, font, Rgba32.White, point);
                startingCenterY += 110;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            using (Stream fileStream = File.Create(destinationPath))
            {
                image.SaveAsJpeg(fileStream);
            }
        }


        /// <summary>
        /// Split a string.
        /// Derived from https://stackoverflow.com/a/16504017/1633757 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<string> WordSplit(string text, int maxLineLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            text = text.Replace("  ", " ");
            var words = text.Split(' ');
            var sb = new StringBuilder();
            var currString = new StringBuilder();

            foreach (var word in words)
            {
                if (currString.Length + word.Length + 1 < maxLineLength) // The + 1 accounts for spaces
                {
                    sb.AppendFormat(" {0}", word);
                    currString.AppendFormat(" {0}", word);
                }
                else
                {
                    currString.Clear();
                    sb.AppendFormat("{0}{1}", Environment.NewLine, word);
                    currString.AppendFormat(" {0}", word);
                }
            }
            return sb.ToString().TrimStart().TrimEnd().Split("\n").ToList();
        }

        /// <summary>
        /// Deletes all files from a directory
        /// </summary>
        /// <param name="path"></param>
        public static void EmptyDirectory(string path)
        {
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles();
            foreach (System.IO.FileInfo file in files)
            {
                file.Delete();
            }
        }

        /// <summary>
        /// Use linux slashes, make sure ends in slash
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizePath(string path, bool isFile)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            path = path.Replace('/', Path.DirectorySeparatorChar);
            if (isFile == false && path.EndsWith(Path.DirectorySeparatorChar) == false)
            {
                path = path + Path.DirectorySeparatorChar;
            }
            return path;
        }
    }
}