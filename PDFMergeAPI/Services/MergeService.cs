using PDFMergeAPI.Helper;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMergeAPI.Services
{
    public class MergeService : IMergeService
    {
        private readonly string path = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        public async Task<string> MergeFilesAsync(List<FileUploadResponse> files)
        {
            var outputPath = Path.Combine(path, $"{Guid.NewGuid()}.pdf");
            var standardSize = PageSize.A4;

            return await Task.Run(() =>
            {
                using var outputDocument = new PdfDocument();

                foreach (var file in files)
                {
                    var filePath = Path.Combine(path, file.FileName);
                    using var form = XPdfForm.FromFile(filePath);

                    for (int i = 0; i < form.PageCount; i++)
                    {
                        form.PageNumber = i + 1;

                        var newPage = outputDocument.AddPage();
                        newPage.Size = standardSize;

                        using (XGraphics gfx = XGraphics.FromPdfPage(newPage))
                        {
                            double scale = Math.Min(newPage.Width / form.PointWidth, newPage.Height / form.PointHeight);

                            double width = form.PointWidth * scale;
                            double height = form.PointHeight * scale;

                            double x = (newPage.Width - width) / 2;
                            double y = (newPage.Height - height) / 2;

                            gfx.DrawImage(form, x, y, width, height);
                        }
                    }
                }

                outputDocument.Save(outputPath);
                return outputPath;
            });
        }
    }
}
