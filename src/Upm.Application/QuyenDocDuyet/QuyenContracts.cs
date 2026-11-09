using Upm.Domain.Enums;

namespace Upm.Application.QuyenDocDuyet;

// ===================== UC-3b · Nộp quyển, ý kiến GVHD, đọc duyệt GVĐD, sửa gấp, đạo văn =====================

/// <summary>Kiểm tra đạo văn. Cài đặt giả lập (có seed) hoặc phần mềm ngoài qua nhập tay kết quả.</summary>
public interface IPlagiarismChecker
{
    Task<KetQuaQuetDaoVan> QuetAsync(Stream noiDung, CancellationToken ct = default);
}

public sealed record KetQuaQuetDaoVan(string PhanMemQuet, decimal TyLeTrungPct);

public sealed record PhienBanQuyenItem(
    long Id,
    long SVTrongDotId,
    int SoPhienBan,
    LoaiBanQuyen LoaiBan,
    string TenFile,
    long KichThuocByte,
    string HashSHA256,
    DateTime NgayNop,
    bool NopQuaHan,
    bool LaBanBaoVe);

public sealed record YKienGvhdRequest(bool DongY, string? LyDoKhongDongY, decimal? DiemGVHD, decimal? DiemKhac);

public sealed record YKienGvhdItem(long Id, long SVTrongDotId, bool DongY, string? LyDoKhongDongY, decimal? DiemGVHD, decimal? DiemKhac, DateTime NgayCho);

public sealed record DocDuyetRequest(KetQuaDocDuyetLoai KetQua, string? NhanXet, decimal? DiemGVDD, DateOnly? HanSuaDen);

public sealed record DocDuyetItem(
    long Id,
    long SVTrongDotId,
    long PhienBanQuyenId,
    int SoPhienBan,
    byte VongDoc,
    KetQuaDocDuyetLoai KetQua,
    string? NhanXet,
    decimal? DiemGVDD,
    DateOnly? HanSuaDen,
    DateTime? NgayQuyet);

/// <summary>Quyển cần GVĐD đọc: hạn đọc theo số ngày cấu hình kể từ khi nộp, còn lại bao nhiêu ngày.</summary>
public sealed record QuyenCanDocItem(
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    long PhienBanQuyenId,
    int SoPhienBan,
    LoaiBanQuyen LoaiBan,
    string TenFile,
    DateTime NgayNop,
    DateOnly HanDoc,
    int ConLaiNgay,
    int VongDoc);

public sealed record NhapTayDaoVanRequest(string PhanMemQuet, decimal TyLeTrungPct);

public sealed record KiemTraDaoVanItem(
    long Id,
    long PhienBanQuyenId,
    string PhanMemQuet,
    decimal TyLeTrungPct,
    decimal NguongApDungPct,
    bool VuotNguong,
    DateTime NgayQuet);

public sealed record KiemTraBaoVeItem(bool DuocBaoVe, IReadOnlyList<string> LyDo);

public sealed record BanBaoVeItem(long PhienBanQuyenId, int SoPhienBan, string HashSHA256);

/// <summary>GVĐD quá hạn đọc (D09): Trưởng BM có thể thay GVĐD; SV không bị tính lỗi.</summary>
public sealed record GvddTreHanItem(
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    long GVDDId,
    string MaGVDD,
    string TenGVDD,
    DateTime NgayNopBan,
    int SoNgayQuaHan);

public sealed record ThayGvddRequest(long GiangVienMoiId, string LyDo);

public interface IQuyenService
{
    Task<PhienBanQuyenItem> NopQuyenAsync(long svTrongDotId, Stream file, string tenFile, string contentType, CancellationToken ct = default);
    Task<IReadOnlyList<PhienBanQuyenItem>> LichSuPhienBanAsync(long svTrongDotId, CancellationToken ct = default);
    Task<(Stream Noi, string ContentType, string TenFile)> TaiPhienBanAsync(long phienBanId, CancellationToken ct = default);
    Task<YKienGvhdItem> YKienGvhdAsync(long svTrongDotId, YKienGvhdRequest request, CancellationToken ct = default);
    Task<DocDuyetItem> DocDuyetAsync(long svTrongDotId, DocDuyetRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuyenCanDocItem>> QuyenCanDocAsync(long dotId, CancellationToken ct = default);
    Task<KiemTraDaoVanItem> NhapTayDaoVanAsync(long svTrongDotId, NhapTayDaoVanRequest request, CancellationToken ct = default);
    Task<KiemTraBaoVeItem> KiemTraBaoVeAsync(long svTrongDotId, CancellationToken ct = default);
    Task<BanBaoVeItem> DanhDauBanBaoVeAsync(long svTrongDotId, CancellationToken ct = default);
    Task<IReadOnlyList<GvddTreHanItem>> GvddTreHanAsync(long dotId, CancellationToken ct = default);
    Task ThayGvddAsync(long svTrongDotId, ThayGvddRequest request, CancellationToken ct = default);
}
