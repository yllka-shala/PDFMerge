using PDFMergeAPI.Helper;

namespace PDFMergeAPI.Services
{
    public interface IMergeService
    {
        Task<string> MergeFilesAsync(List<FileUploadResponse> files);
    }
        
}
