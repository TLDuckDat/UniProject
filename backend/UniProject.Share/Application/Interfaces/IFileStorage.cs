namespace UniProject.Share.Application.Interfaces
{
    public interface IFileStorage
    {
        /// <summary>
        /// Lưu tệp và trả về đường dẫn tương đối so với nơi lưu trữ.
        /// </summary>
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string subDirectory, CancellationToken cancellationToken = default);
        Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(string relativeFilePath, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string relativeFilePath, CancellationToken cancellationToken = default);
    }
}
