namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ISupaBaseFileService
    {
        Task<string> UploadFileAsync(string bucketName, string fileName, Stream fileStream, string contentType);
        Task<bool> DeleteFileAsync(string fileUrlOrPath, string bucketName);

        // ── NEW ──
        Task<byte[]?> DownloadFileAsync(string fileUrlOrPath, string bucketName);
    }
}
