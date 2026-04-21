using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using PDFMergeAPI.Helper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace PDFMergeAPI.Services
{
    public class FileService : IFileService
    {
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5 MB
        private readonly string[] _validExtensions = [".png", ".jpg", ".gif", ".pdf"];
        private readonly string path = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public async Task<List<FileUploadResponse>> UploadFilesAsync(List<IFormFile> files)
        {
            var uploadedFiles = new List<FileUploadResponse>();

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var file in files)
            {

                var validation = ValidateFile(file);
                if (!validation.IsSuccess)
                {
                    throw new InvalidOperationException(validation.Message);
                }

                string extention = Path.GetExtension(file.FileName);
                string fileName = Guid.NewGuid().ToString() + extention;               

                using FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create);
                await file.CopyToAsync(stream);

                var result = new FileUploadResponse
                {
                    FileName = fileName,
                    IsSuccess = validation.IsSuccess,
                    Message = validation.Message,
                };

                uploadedFiles.Add(result);
            }

            return uploadedFiles;
        }

        public FileUploadResponse ValidateFile(IFormFile file)
        {
            if (file == null)
            {
               return new FileUploadResponse { FileName = file.FileName, IsSuccess = false, Message = "No file uploaded!" };
            }

            string extention = Path.GetExtension(file.FileName);
            if (!_validExtensions.Contains(extention))
            {
               return new FileUploadResponse { FileName = file.FileName, IsSuccess = false, Message = $"Extension is not valid ({string.Join(',', _validExtensions)})!" };
            }

            long size = file.Length;
            if (size > _maxFileSize)
            {
               return new FileUploadResponse { FileName = file.FileName, IsSuccess = false, Message = "Maximum size can be 5mb!" };
            }

            return new FileUploadResponse { FileName = file.FileName, IsSuccess = true, Message = "File uploaded successfully!" };
        }

        public FileUploadResponse DeleteFile(string fileName)
        {
            string filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("File doesn't exist!");
            }

            File.Delete(filePath);
            return new FileUploadResponse { FileName = fileName, IsSuccess = true, Message = "File deleted successfully!" };
        }

        public async Task<FileResult> DownloadFileAsync(string fileName)
        {
            string filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("File doesn't exist!");
            }

            var contentType = "application/octet-stream";
            var bytes = await File.ReadAllBytesAsync(filePath);

            return new FileContentResult(bytes, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }
}
