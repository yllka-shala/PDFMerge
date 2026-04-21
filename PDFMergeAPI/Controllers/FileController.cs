using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFMergeAPI.Services;

namespace PDFMergeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IMergeService _mergeService;
        private readonly IFileService _fileService;
        private readonly IConvertService _convertService;

        public FileController(IMergeService mergeService, IFileService fileService, IConvertService convertService)
        {
            _mergeService = mergeService;
            _fileService = fileService;
            _convertService = convertService;
        }

        [HttpPost("MergeFiles")]
        public async Task<IActionResult> MergeFiles([FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest("Invalid File!");

            var fileItems = await _fileService.UploadFilesAsync(files);
            var mergedPath = await _mergeService.MergeFilesAsync(fileItems);

            var bytes = await System.IO.File.ReadAllBytesAsync(mergedPath);
            return File(bytes, "application/pdf", "Merged.pdf");

        }

        [HttpPost("UploadFiles")]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest("InvalidFile!");

            var uploadedFile = await _fileService.UploadFilesAsync(files);
            return Ok(uploadedFile);
        }

        [HttpDelete("DeleteFiles")]
        public IActionResult DeleteFile(string fileName)
        {
            var result = _fileService.DeleteFile(fileName);
            return Ok(result);
        }

        [HttpGet("DownloadFile/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var fileResult = await _fileService.DownloadFileAsync(fileName);

            if (fileResult == null)
                return NotFound();

            return fileResult;
        }

        [HttpPost("ConvertImagesToPdf")]
        public async Task<IActionResult> ConvertToPdf(IFormFile file)
        {
            var pdfBytes = await _convertService.ConvertImageToPdfAsync(file);
            return File(pdfBytes, "application/pdf", "Converted.pdf");
        }
    }
}
