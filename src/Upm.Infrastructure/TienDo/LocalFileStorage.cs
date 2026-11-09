using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Upm.Application.TienDo;
using Upm.Domain.Common;

namespace Upm.Infrastructure.TienDo;

/// <summary>Cấu hình lưu file: <c>Files:Root</c> (mặc định App_Data/tep cạnh assembly) và giới hạn dung lượng.</summary>
public sealed class TepOptions
{
    public const string Section = "Files";
    public string? Root { get; set; }
    /// <summary>Giới hạn dung lượng một file (MB) – quyển và báo cáo.</summary>
    public int KichThuocToiDaMB { get; set; } = 50;
}

/// <summary>
/// Cài đặt <see cref="IFileStorage"/> trên ổ đĩa. Khóa lưu trữ = năm/tháng/GUID.ext; không dùng tên gốc từ client
/// (chống path traversal). Hash SHA-256 tính khi ghi.
/// </summary>
public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private static readonly char[] KyTuLaCam = Path.GetInvalidFileNameChars();
    private readonly string _root = configuration["Files:Root"] is { Length: > 0 } r
        ? r
        : Path.Combine(AppContext.BaseDirectory, "App_Data", "tep");

    public async Task<TepLuuKetQua> LuuAsync(Stream noiDung, string tenGoc, string contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(noiDung);
        var ext = Path.GetExtension(tenGoc).ToLowerInvariant();
        if (ext.Length is 0 or > 10 || ext.AsSpan(1).ContainsAny(KyTuLaCam)) ext = string.Empty;
        var khoa = Path.Combine(DateTime.UtcNow.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("MM", System.Globalization.CultureInfo.InvariantCulture),
            Guid.NewGuid().ToString("N") + ext);
        var duongDan = Path.Combine(_root, khoa);
        Directory.CreateDirectory(Path.GetDirectoryName(duongDan)!);

        using var sha = SHA256.Create();
        long tong = 0;
        await using (var ghi = new FileStream(duongDan, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            int doc;
            while ((doc = await noiDung.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                sha.TransformBlock(buffer, 0, doc, null, 0);
                await ghi.WriteAsync(buffer.AsMemory(0, doc), ct);
                tong += doc;
            }
        }
        sha.TransformFinalBlock([], 0, 0);
        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        return new TepLuuKetQua(khoa.Replace(Path.DirectorySeparatorChar, '/'), tong, hash);
    }

    public Task<Stream> DocAsync(string duongDan, CancellationToken ct = default)
    {
        var full = Path.GetFullPath(Path.Combine(_root, duongDan));
        if (!full.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UPM_FILE_DUONG_DAN_KHONG_HOP_LE", "Đường dẫn file không hợp lệ.");
        Stream s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(s);
    }
}
