namespace Upm.Application.DongBo;

public enum LoaiImport { SinhVien, BangDiem, KyLuatSV, ThongTinTienDoSV }
public sealed record ImportRowError(int Row, string Field, string Message);
public sealed record ImportPreview(Guid PreviewId, LoaiImport Loai, string FileName, int TotalRows, int ValidRows,
    IReadOnlyList<ImportRowError> Errors, DateTime ExpiresAtUtc);
public sealed record ImportCommitResult(int Inserted, int Updated, int Skipped);

public interface IQldtGateway
{
    byte[] TaoFileMau(LoaiImport loai);
    Task<ImportPreview> XemTruocAsync(LoaiImport loai, string fileName, Stream file, CancellationToken ct = default);
    Task<ImportCommitResult> XacNhanAsync(Guid previewId, CancellationToken ct = default);
}
