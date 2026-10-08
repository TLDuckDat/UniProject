using System.Text.Json;

namespace UniProject.API.Data
{
    /// <summary>
    /// File dữ liệu seed. Dữ liệu thật (sinh viên, giảng viên, đề tài, báo cáo) đặt ở Data/Seed/, không commit lên Git;
    /// thiếu file nào thì dùng file cùng tên trong Data/Seed.example/ (dữ liệu hư cấu, có trong Git), không có cả hai thì bỏ qua.
    /// </summary>
    public static class SeedFiles
    {
        public const string RealFolder = "Seed";
        public const string ExampleFolder = "Seed.example";

        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

        /// <summary>Đường dẫn file theo thứ tự: Data/Seed rồi Data/Seed.example, ở content root trước rồi thư mục chạy.</summary>
        public static string? Find(string contentRoot, string fileName) =>
            new[] { contentRoot, AppContext.BaseDirectory }
                .SelectMany(root => new[] { RealFolder, ExampleFolder }.Select(folder => Path.Combine(root, "Data", folder, fileName)))
                .FirstOrDefault(File.Exists);

        /// <summary>Thư mục chứa file seed vừa tìm được, dùng để lấy file đi kèm (vd báo cáo PDF).</summary>
        public static string FolderOf(string filePath) => Path.GetDirectoryName(filePath)!;

        public static async Task<T?> ReadAsync<T>(string contentRoot, string fileName, ILogger logger) where T : class
        {
            var path = Find(contentRoot, fileName);
            if (path == null)
            {
                logger.LogWarning("Không thấy Data/{Real}/{File} hoặc Data/{Example}/{File}, bỏ qua phần seed liên quan",
                    RealFolder, fileName, ExampleFolder, fileName);
                return null;
            }
            if (path.Contains(Path.DirectorySeparatorChar + ExampleFolder + Path.DirectorySeparatorChar))
                logger.LogInformation("Seed {File} từ dữ liệu mẫu (Data/{Example})", fileName, ExampleFolder);
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), Options);
        }
    }
}
