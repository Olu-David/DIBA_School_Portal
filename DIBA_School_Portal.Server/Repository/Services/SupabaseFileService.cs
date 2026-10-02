using Amazon.S3;
using Amazon.S3.Model;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineVotingApplication.Services
{
    public class SupabaseFileService : ISupaBaseFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupabaseFileService> _logger;

        public SupabaseFileService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<SupabaseFileService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(string bucketName, string fileName, Stream fileStream, string contentType)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                throw new ArgumentException("File stream cannot be empty.", nameof(fileStream));
            }

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,          
                InputStream = fileStream,
                ContentType = contentType
            };

            // This triggers the upload via S3 protocol to Supabase
            await _s3Client.PutObjectAsync(putRequest);

            // Construct the public absolute URL for Supabase storage
            var supabaseUrl = (_configuration["Supabase:Url"] ?? _configuration["SupabaseUrl"] ?? "").TrimEnd('/');

            return $"{supabaseUrl}/storage/v1/object/public/{bucketName}/{fileName}";
        }

        public async Task<bool> DeleteFileAsync(string fileUrlOrPath, string bucketName)
        {
            try
            {
                var uri = new Uri(fileUrlOrPath, UriKind.RelativeOrAbsolute);
                string filePath = uri.IsAbsoluteUri ? uri.Segments.Last() : fileUrlOrPath;
                filePath = filePath.TrimStart('/');

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = filePath
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);

                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                       response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from Supabase S3 storage: {Path}", fileUrlOrPath);
                return false;
            }
        }
        public async Task<byte[]?> DownloadFileAsync(string fileUrlOrPath, string bucketName)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrPath))
                return null;

            try
            {
                var key = ExtractObjectKey(fileUrlOrPath, bucketName);

                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("Could not extract object key from: {Path}", fileUrlOrPath);
                    return null;
                }

                using var response = await _s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                });

                using var ms = new MemoryStream();
                await response.ResponseStream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch (AmazonS3Exception s3Ex) when (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found in Supabase bucket '{Bucket}': {Path}",
                    bucketName, fileUrlOrPath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from Supabase: {Path}", fileUrlOrPath);
                return null;
            }
        }

        // Helper — extracts "folder/file.jpg" from any Supabase URL shape
        private static string ExtractObjectKey(string urlOrPath, string bucketName)
        {
            var input = urlOrPath.Trim();

            // Absolute URL
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath.TrimStart('/');

                var prefixes = new[]
                {
            $"storage/v1/object/public/{bucketName}/",
            $"storage/v1/object/sign/{bucketName}/",
            $"storage/v1/object/{bucketName}/",
        };

                foreach (var prefix in prefixes)
                {
                    var idx = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        return path[(idx + prefix.Length)..].TrimStart('/');
                }

                // Fallback — find "bucket/" anywhere
                var bucketSegment = $"{bucketName}/";
                var bucketIdx = path.IndexOf(bucketSegment, StringComparison.OrdinalIgnoreCase);
                if (bucketIdx >= 0)
                    return path[(bucketIdx + bucketSegment.Length)..].TrimStart('/');

                return path.TrimStart('/');
            }

            // Relative path
            var relative = input.TrimStart('/');
            if (relative.StartsWith($"{bucketName}/", StringComparison.OrdinalIgnoreCase))
                relative = relative[(bucketName.Length + 1)..];

            return relative;
        }
    }
}