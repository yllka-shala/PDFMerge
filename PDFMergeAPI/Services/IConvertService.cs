namespace PDFMergeAPI.Services
{
    public interface IConvertService
    {
        Task<byte[]> ConvertImageToPdfAsync(IFormFile imageFile);
    }
}
