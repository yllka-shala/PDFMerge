using Microsoft.AspNetCore.Mvc;
using PDFMergeAPI.Helper;

namespace PDFMergeAPI.Services
{
    public interface IFileService
    {
        Task<List<FileUploadResponse>> UploadFilesAsync(List<IFormFile> files);
        FileUploadResponse ValidateFile(IFormFile file);
        FileUploadResponse DeleteFile(string fileName);
        Task<FileResult> DownloadFileAsync(string fileName);
    }
}
