using System.IO;
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

            var point = new PointF(5, 700);
            Font font = new Font(this.FontFamily, 100f, FontStyle.Regular);

            Image<Rgba32> image = new Image<Rgba32>(1000, 1500);
            image.BackgroundColor(Rgba32.White);

            image.DrawText(text, font, Rgba32.Red, point);
            using (Stream fileStream = File.Create(destinationPath))
            {
                image.SaveAsJpeg(fileStream);
            }
        }
    }
}