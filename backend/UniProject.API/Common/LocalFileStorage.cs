using UniProject.Share.Application.Interfaces;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Common
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _baseDirectory;
        private readonly ILogger<LocalFileStorage> _logger;

        public LocalFileStorage(IWebHostEnvironment environment, ILogger<LocalFileStorage> logger)
        {
            _baseDirectory = Path.Combine(environment.ContentRootPath, "uploads");
            _logger = logger;
            Directory.CreateDirectory(_baseDirectory);
        }

        /// <summary>
        /// Thêm tiền tố Guid vào tên file để tránh ghi đè; trả về đường dẫn tương đối để lưu vào CSDL.
        /// </summary>
        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subDirectory, CancellationToken cancellationToken = default)
        {
            var targetFolder = Path.Combine(_baseDirectory, subDirectory);
            Directory.CreateDirectory(targetFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var fullPath = Path.Combine(targetFolder, uniqueFileName);

            using (var output = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(output, cancellationToken);
            }

            _logger.LogInformation("Saved file {FileName} to {FullPath}", fileName, fullPath);
            return Path.Combine(subDirectory, uniqueFileName).Replace('\\', '/');
        }

        public Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(string relativeFilePath, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_baseDirectory, relativeFilePath);
            if (!File.Exists(fullPath))
            {
                throw new NotFoundException("Tệp tin", relativeFilePath);
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = Path.GetFileName(fullPath);

            return Task.FromResult((Stream: (Stream)stream, ContentType: GetContentType(fileName), FileName: fileName));
        }

        public Task<bool> DeleteFileAsync(string relativeFilePath, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_baseDirectory, relativeFilePath);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {FullPath}", fullPath);
            return Task.FromResult(true);
        }

        private static string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}
