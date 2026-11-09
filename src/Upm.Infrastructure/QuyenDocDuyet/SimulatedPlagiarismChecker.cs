using System.Security.Cryptography;
using Upm.Application.QuyenDocDuyet;

namespace Upm.Infrastructure.QuyenDocDuyet;

/// <summary>
/// Cài đặt GIẢ LẬP kiểm tra đạo văn (dev/test): tỉ lệ trùng 0–40% xác định theo SHA-256 của nội dung (có seed).
/// Thay bằng phần mềm thật khi có tích hợp; khoa dùng phần mềm ngoài nhập tay qua API.
/// </summary>
public sealed class SimulatedPlagiarismChecker : IPlagiarismChecker
{
    public async Task<KetQuaQuetDaoVan> QuetAsync(Stream noiDung, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await noiDung.CopyToAsync(ms, ct);
        var hash = SHA256.HashData(ms.ToArray());
        var tyLe = (decimal)(BitConverter.ToUInt32(hash, 0) % 41);
        return new KetQuaQuetDaoVan("Giả lập (seed SHA-256)", tyLe);
    }
}
