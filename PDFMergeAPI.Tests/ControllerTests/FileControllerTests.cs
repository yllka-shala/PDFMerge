using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PDFMergeAPI.Controllers;
using PDFMergeAPI.Helper;
using PDFMergeAPI.Services;

namespace PDFMergeAPI.Tests.ControllerTests
{
    public class FileControllerTests
    {
        private readonly Mock<IMergeService> _mergeService = new();
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IConvertService> _convertService = new();
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _controller = new FileController(_mergeService.Object, _fileService.Object, _convertService.Object);
        }

        [Fact]
        public async Task MergeFiles_WithFiles_ReturnsMergedPdf()
        {
            var files = new List<IFormFile> { CreateFormFile("first.pdf") };
            var uploadedFiles = new List<FileUploadResponse>
            {
                new() { FileName = "first.pdf", IsSuccess = true }
            };
            var mergedPdfBytes = new byte[] { 1, 2, 3, 4 };
            var mergedPath = Path.GetTempFileName();
            await System.IO.File.WriteAllBytesAsync(mergedPath, mergedPdfBytes);

            _fileService.Setup(service => service.UploadFilesAsync(files)).ReturnsAsync(uploadedFiles);
            _mergeService.Setup(service => service.MergeFilesAsync(uploadedFiles)).ReturnsAsync(mergedPath);

            try
            {
                var result = await _controller.MergeFiles(files);

                var fileResult = Assert.IsType<FileContentResult>(result);
                Assert.Equal(mergedPdfBytes, fileResult.FileContents);
                Assert.Equal("application/pdf", fileResult.ContentType);
                Assert.Equal("Merged.pdf", fileResult.FileDownloadName);
                _fileService.Verify(service => service.UploadFilesAsync(files), Times.Once);
                _mergeService.Verify(service => service.MergeFilesAsync(uploadedFiles), Times.Once);
            }
            finally
            {
                System.IO.File.Delete(mergedPath);
            }
        }

        [Fact]
        public async Task MergeFiles_WithoutFiles_ReturnsBadRequest()
        {
            var result = await _controller.MergeFiles(new List<IFormFile>());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid File!", badRequest.Value);
            _fileService.Verify(service => service.UploadFilesAsync(It.IsAny<List<IFormFile>>()), Times.Never);
            _mergeService.Verify(service => service.MergeFilesAsync(It.IsAny<List<FileUploadResponse>>()), Times.Never);
        }

        [Fact]
        public async Task UploadFile_WithFiles_ReturnsUploadedFiles()
        {
            var files = new List<IFormFile> { CreateFormFile("report.pdf") };
            var uploadedFiles = new List<FileUploadResponse>
            {
                new() { FileName = "report.pdf", IsSuccess = true, Message = "File uploaded successfully!" }
            };
            _fileService.Setup(service => service.UploadFilesAsync(files)).ReturnsAsync(uploadedFiles);

            var result = await _controller.UploadFile(files);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(uploadedFiles, okResult.Value);
            _fileService.Verify(service => service.UploadFilesAsync(files), Times.Once);
        }

        [Fact]
        public async Task UploadFile_WithoutFiles_ReturnsBadRequest()
        {
            var result = await _controller.UploadFile(new List<IFormFile>());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("InvalidFile!", badRequest.Value);
            _fileService.Verify(service => service.UploadFilesAsync(It.IsAny<List<IFormFile>>()), Times.Never);
        }

        [Fact]
        public void DeleteFile_ReturnsServiceResponse()
        {
            const string fileName = "report.pdf";
            var response = new FileUploadResponse
            {
                FileName = fileName,
                IsSuccess = true,
                Message = "File deleted successfully!"
            };
            _fileService.Setup(service => service.DeleteFile(fileName)).Returns(response);

            var result = _controller.DeleteFile(fileName);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, okResult.Value);
            _fileService.Verify(service => service.DeleteFile(fileName), Times.Once);
        }

        [Fact]
        public void DeleteFile_WhenUnsuccessful_ReturnsFailureResponse()
        {
            const string fileName = "missing.pdf";
            var response = new FileUploadResponse
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "File could not be deleted."
            };
            _fileService.Setup(service => service.DeleteFile(fileName)).Returns(response);

            var result = _controller.DeleteFile(fileName);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, okResult.Value);
            Assert.False(((FileUploadResponse)okResult.Value!).IsSuccess);
            _fileService.Verify(service => service.DeleteFile(fileName), Times.Once);
        }

        [Fact]
        public async Task DownloadFile_WhenFound_ReturnsFileResult()
        {
            const string fileName = "report.pdf";
            var expected = new FileContentResult(new byte[] { 5, 6, 7 }, "application/pdf")
            {
                FileDownloadName = fileName
            };
            _fileService.Setup(service => service.DownloadFileAsync(fileName)).ReturnsAsync(expected);

            var result = await _controller.DownloadFile(fileName);

            Assert.Same(expected, result);
            _fileService.Verify(service => service.DownloadFileAsync(fileName), Times.Once);
        }

        [Fact]
        public async Task DownloadFile_WhenNotFound_ReturnsNotFound()
        {
            const string fileName = "missing.pdf";
            _fileService.Setup(service => service.DownloadFileAsync(fileName)).ReturnsAsync((FileResult)null!);

            var result = await _controller.DownloadFile(fileName);

            Assert.IsType<NotFoundResult>(result);
            _fileService.Verify(service => service.DownloadFileAsync(fileName), Times.Once);
        }

        [Fact]
        public async Task ConvertToPdf_ReturnsConvertedPdf()
        {
            var image = CreateFormFile("image.png");
            var pdfBytes = new byte[] { 8, 9, 10 };
            _convertService.Setup(service => service.ConvertImageToPdfAsync(image)).ReturnsAsync(pdfBytes);

            var result = await _controller.ConvertToPdf(image);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(pdfBytes, fileResult.FileContents);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("Converted.pdf", fileResult.FileDownloadName);
            _convertService.Verify(service => service.ConvertImageToPdfAsync(image), Times.Once);
        }

        [Fact]
        public async Task ConvertToPdf_WhenConversionFails_PropagatesException()
        {
            var image = CreateFormFile("invalid.png");
            var exception = new InvalidOperationException("Image conversion failed.");
            _convertService.Setup(service => service.ConvertImageToPdfAsync(image)).ThrowsAsync(exception);

            var result = await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.ConvertToPdf(image));

            Assert.Same(exception, result);
            _convertService.Verify(service => service.ConvertImageToPdfAsync(image), Times.Once);
        }

        private static IFormFile CreateFormFile(string fileName)
        {
            var content = new byte[] { 1, 2, 3 };
            return new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);
        }
    }
}
