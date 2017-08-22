using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public void CreateTextPoster(string text, int width, int height, string destinationPath)
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
        /// Get the full base url pointing to the root of this api
        /// </summary>
        /// <returns></returns>
        public static string BaseUrl
        {
            get
            {
                var store = Middleware.RequestMiddleware.CurrentHttpContext.Items;
                var request = Middleware.RequestMiddleware.CurrentHttpContext.Request;
                if (store.ContainsKey("baseUrl") == false)
                {
                    var url = $"{request.Scheme}://{request.Host}{request.Path}";
                    //remove anything after and including /api/
                    var baseUrl = url.Substring(0, url.ToLowerInvariant().IndexOf("/api/") + 1);
                    store["baseUrl"] = baseUrl;
                }
                return (string)store["baseUrl"];
            }
        }
    }
}