using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Drawing;
using System.Drawing.Imaging;

namespace PDFMergeAPI.Services
{
    public class ConvertService : IConvertService
    {
        public async Task<byte[]> ConvertImageToPdfAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new InvalidOperationException("Invalid image file.");

            using var imageStream = imageFile.OpenReadStream();
            using var bitmap = new Bitmap(imageStream);

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = bitmap.Width;
            page.Height = bitmap.Height;

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                ms.Position = 0;
                var img = XImage.FromStream(ms);

                gfx.DrawImage(img, 0, 0, bitmap.Width, bitmap.Height);
            }

            using var outputStream = new MemoryStream();
            doc.Save(outputStream, false);
            return outputStream.ToArray();
        }
    }
}
