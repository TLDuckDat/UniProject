using Upm.Domain.Enums;

namespace Upm.Application.TienDo;

// ===================== UC-3a · Phiếu giao nhiệm vụ, tiến độ 3 mốc, đơn, thông báo =====================

/// <summary>Lưu file qua interface (mặc định ổ đĩa local; sau này có thể đổi sang object storage).</summary>
public interface IFileStorage
{
    Task<TepLuuKetQua> LuuAsync(Stream noiDung, string tenGoc, string contentType, CancellationToken ct = default);
    Task<Stream> DocAsync(string duongDan, CancellationToken ct = default);
}

public sealed record TepLuuKetQua(string DuongDan, long KichThuocByte, string HashSHA256);

/// <summary>Gửi email. Dev: SMTP → Mailpit. Test: bản ghi lại trong bộ nhớ.</summary>
public interface IEmailSender
{
    Task GuiAsync(string denEmail, string tieuDe, string noiDung, CancellationToken ct = default);
}

public sealed record MocTienDoItem(
    long Id,
    long DotId,
    MaMocTienDo MaMoc,
    byte TuanTu,
    byte TuanDen,
    DateOnly NgayHan,
    decimal? YeuCauToiThieuPct,
    string? MoTa);

public sealed record CapNhatMocRequest(DateOnly NgayHan, decimal? YeuCauToiThieuPct, string? MoTa);

public sealed record PhieuGiaoNhiemVuRequest(string NoiDungNhiemVu, string? YeuCauSanPham, DateOnly NgayGiao);

public sealed record PhieuGiaoNhiemVuItem(
    long Id,
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    string NoiDungNhiemVu,
    string? YeuCauSanPham,
    DateOnly NgayGiao);

public sealed record BaoCaoTienDoItem(
    long Id,
    long MocTienDoId,
    DateTime NgayNop,
    long? FileId,
    string? TenFile,
    decimal TyLeHoanThanhPct,
    KetQuaTienDo KetQua,
    decimal? DiemTienDo,
    string? GVHDNhanXet,
    bool CamKetAI);

/// <summary>Một mốc của một hồ sơ: mốc, hạn hiệu lực (kể cả gia hạn), báo cáo mới nhất và trạng thái.</summary>
public sealed record TrangThaiMocItem(
    long MocTienDoId,
    MaMocTienDo MaMoc,
    DateOnly HanHieuLuc,
    bool DaGiaHan,
    decimal? YeuCauToiThieuPct,
    BaoCaoTienDoItem? BaoCaoMoiNhat,
    KetQuaTienDo? TrangThai);

public sealed record HoSoTienDoItem(
    long SVTrongDotId,
    long SinhVienId,
    string MaSV,
    string HoTen,
    TrangThaiSVTrongDot TrangThaiHoSo,
    string? TenGVHD,
    long? GVHDId,
    bool CoPhieu,
    IReadOnlyList<TrangThaiMocItem> CacMoc,
    bool CanNhacRut,
    bool CoDonDangCho);

public sealed record NopBaoCaoRequest(decimal TyLeHoanThanhPct, bool CamKetAI);

public sealed record NhanXetBaoCaoRequest(decimal? DiemTienDo, string? NhanXet);

public sealed record DonTienDoTaoRequest(LoaiDon LoaiDon, string NoiDung);

public sealed record DonTienDoItem(
    long DonTuId,
    LoaiDon LoaiDon,
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    string NoiDung,
    DateTime NgayGui,
    TrangThaiDonTu TrangThai,
    string? KetQuaXuLy,
    DateOnly? HanMoi);

public sealed record DuyetDonTienDoRequest(bool DongY, DateOnly? HanMoi, string? LyDo);

public sealed record ThongBaoItem(long Id, string TieuDe, string NoiDung, bool DaDoc, DateTime NgayTao);

/// <summary>Báo cáo tổng hợp cho Bộ môn: SV chậm tiến độ (CanhBao/KhongNop).</summary>
public sealed record SvCham(long SVTrongDotId, string MaSV, string HoTen, string? TenGVHD, int SoCanhBao, int SoKhongNop, bool CanNhacRut);

public interface ITienDoService
{
    Task<IReadOnlyList<MocTienDoItem>> DanhSachMocAsync(long dotId, CancellationToken ct = default);
    Task<MocTienDoItem> CapNhatMocAsync(long mocId, CapNhatMocRequest request, CancellationToken ct = default);
    Task<PhieuGiaoNhiemVuItem?> LayPhieuAsync(long svTrongDotId, CancellationToken ct = default);
    Task<PhieuGiaoNhiemVuItem> LuuPhieuAsync(long svTrongDotId, PhieuGiaoNhiemVuRequest request, CancellationToken ct = default);
    Task<(byte[] Noi, string TenFile)> XuatPhieuPdfAsync(long svTrongDotId, CancellationToken ct = default);
    Task<BaoCaoTienDoItem> NopBaoCaoAsync(long svTrongDotId, long mocId, Stream file, string tenFile, string contentType,
        NopBaoCaoRequest request, CancellationToken ct = default);
    Task<BaoCaoTienDoItem> NhanXetBaoCaoAsync(long baoCaoId, NhanXetBaoCaoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<HoSoTienDoItem>> DanhSachGvAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<HoSoTienDoItem>> DanhSachBoMonAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<SvCham>> SvChamAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<HoSoTienDoItem>> TienDoCuaToiAsync(long dotId, CancellationToken ct = default);
    Task<DonTienDoItem> TaoDonAsync(long svTrongDotId, DonTienDoTaoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DonTienDoItem>> DanhSachDonAsync(long dotId, CancellationToken ct = default);
    Task DuyetDonAsync(long donTuId, DuyetDonTienDoRequest request, CancellationToken ct = default);
    Task TamDungAsync(long svTrongDotId, string lyDo, CancellationToken ct = default);
    Task<IReadOnlyList<ThongBaoItem>> ThongBaoCuaToiAsync(CancellationToken ct = default);
    Task DanhDauDocAsync(long thongBaoId, CancellationToken ct = default);
}

/// <summary>Kết quả một lượt nhắc việc (job hằng ngày).</summary>
public sealed record KetQuaNhacViec(int ChuyenT04, int SoEmail, int SoThongBao, int SoBoQua);

public interface INhacViecService
{
    Task<KetQuaNhacViec> ChayAsync(DateOnly homNay, CancellationToken ct = default);
}
