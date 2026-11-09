using Upm.Application.Common;
using Upm.Domain.Enums;

namespace Upm.Application.Workflow;

// ===================== UC-1 · Đợt ĐATN & xét điều kiện giao =====================

public sealed record DotDATNRequest(
    long CoSoId,
    string MaDot,
    string TenDot,
    string NamHoc,
    byte HocKy,
    DateOnly NgayBatDau,
    byte SoTuan,
    DateOnly NgayNopQuyenHan);

public sealed record DotDATNItem(
    long Id,
    long CoSoId,
    string MaDot,
    string TenDot,
    string NamHoc,
    byte HocKy,
    DateOnly NgayBatDau,
    byte SoTuan,
    DateOnly NgayNopQuyenHan,
    TrangThaiDot TrangThai,
    int SoHoSo);

/// <summary>Một hồ sơ cần thêm vào đợt. <c>HoSoTruocId</c> null → tự liên kết hồ sơ làm lại gần nhất nếu có (A21).</summary>
public sealed record HoSoTrongDotRequest(long SinhVienId, long CTDTId, long? HoSoTruocId = null);

/// <summary>Thêm mọi SV ứng viên hợp lệ theo bộ lọc (khóa, ngành, từ khóa); bỏ qua SV không được đưa vào đợt (A11).</summary>
public sealed record ThemHoSoTheoLocRequest(int? KhoaHoc, string? Nganh, string? Q);

public sealed record ThemHoSoBoQuaItem(string MaSV, string HoTen, string LyDo);

public sealed record ThemHoSoKetQua(int SoDaThem, IReadOnlyList<ThemHoSoBoQuaItem> BoQua);

public sealed record HoSoXetGiaoItem(
    long Id,
    long SinhVienId,
    string MaSV,
    string HoTen,
    int KhoaHoc,
    string MaCTDT,
    string TenCTDT,
    TrangThaiSVTrongDot TrangThai,
    bool CoNoTinChi,
    bool ChuaQuaTTTN,
    int LanLamLai,
    long? HoSoTruocId,
    bool CoDeTai,
    string? SoQuyetDinhGiao,
    int? TCChuaTichLuy,
    string? KetQuaXet,
    string? MaNgoaiLe,
    string? RuleVersion,
    DateTime? ThoiDiemXet,
    bool CoKhieuNaiChoXuLy,
    string RowVersion);

public sealed record ThongKeTrangThaiItem(TrangThaiSVTrongDot TrangThai, int SoLuong);

public sealed record DanhSachHoSoKetQua(
    IReadOnlyList<HoSoXetGiaoItem> Items,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<ThongKeTrangThaiItem> ThongKe);

public sealed record LocHoSoRequest(
    TrangThaiSVTrongDot? TrangThai = null,
    bool? CoNoTinChi = null,
    bool? ChuaQuaTTTN = null,
    string? KetQuaXet = null,
    string? Q = null,
    int Page = 1,
    int PageSize = 20);

public sealed record SinhVienUngVienItem(
    long SinhVienId,
    string MaSV,
    string HoTen,
    int KhoaHoc,
    long CTDTId,
    string MaCTDT,
    string TenCTDT,
    string Nganh,
    TrangThaiHocTap TrangThaiHocTap,
    bool CoTheThem,
    string? LyDoKhongThem,
    long? HoSoTruocId);

public sealed record ChiTietXetGiaoItem(
    string MaHP,
    string TenHP,
    LoaiHocPhan LoaiHP,
    int SoTC,
    LoaiNo LoaiNo,
    bool BiLoaiTru,
    string? MaNgoaiLe,
    string? LyDo);

/// <summary>
/// Snapshot một lần xét (không sửa, không xóa). G1 = kỷ luật/hình sự, G2 = trạng thái học tập & thời gian tối đa,
/// G3 = TC chưa tích lũy ≤ ngưỡng của RuleSet.
/// </summary>
public sealed record XetGiaoSnapshotItem(
    long Id,
    LoaiXet LoaiXet,
    string MaRuleSet,
    string RuleVersion,
    DateTime ThoiDiemXet,
    string? NguoiChay,
    string KetQua,
    string? MaNgoaiLe,
    int TCChuaTichLuy,
    int MaxTCChuaTichLuy,
    bool? G1KyLuat,
    bool? G2TrangThaiHocTap,
    bool? G3TinChi,
    bool? DaDatTTTN,
    IReadOnlyList<string> Co,
    string? LyDo,
    IReadOnlyList<ChiTietXetGiaoItem> ChiTiets);

public sealed record KhieuNaiItem(
    long Id,
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    string NoiDung,
    DateTime NgayGui,
    TrangThaiDonTu TrangThai,
    string? KetQuaXuLy,
    DateTime? NgayXuLy);

/// <summary>Trang "Kết quả xét giao ĐATN" của sinh viên – mỗi hồ sơ (đợt + CTĐT) một mục.</summary>
public sealed record HoSoCuaToiXetGiaoItem(
    long SVTrongDotId,
    long DotId,
    string MaDot,
    string TenDot,
    string MaCTDT,
    string TenCTDT,
    TrangThaiSVTrongDot TrangThai,
    bool DaCoQuyetDinhGiao,
    bool CoTheKhieuNai,
    XetGiaoSnapshotItem? KetQuaMoiNhat,
    IReadOnlyList<KhieuNaiItem> KhieuNai);

public sealed record DonKhieuNaiRequest(string NoiDung, long? FileMinhChungId = null);
public sealed record DonKhieuNaiKetQua(long DonTuId);
public sealed record XuLyKhieuNaiRequest(bool ChapNhan, string? LyDo);

public sealed record QuyetDinhGiaoRequest(
    string SoQuyetDinh,
    DateOnly NgayKy,
    long? FileScanId,
    IReadOnlyList<long> SVTrongDotIds,
    string? NguoiKy = null);

public sealed record QuyetDinhGiaoItem(
    long Id,
    string SoQuyetDinh,
    DateOnly NgayKy,
    string? NguoiKy,
    long? FileScanId,
    bool DaKhoaDuLieu,
    IReadOnlyList<long> SVTrongDotIds);

public sealed record XetGiaoLoiItem(long SVTrongDotId, string MaSV, string Code, string Message);

public sealed record XetGiaoJobItem(
    string JobId,
    long DotId,
    string TrangThai,
    int DaXuLy,
    int TongSo,
    int SoLoi,
    IReadOnlyList<XetGiaoLoiItem> Loi);

/// <summary>Tiến độ job xét giao báo về sau mỗi hồ sơ.</summary>
public sealed record XetGiaoTienDo(int DaXuLy, int TongSo, IReadOnlyList<XetGiaoLoiItem> Loi);

public interface IDotDATNService
{
    Task<PagedResult<DotDATNItem>> DanhSachDotAsync(string? q, int page, int pageSize, CancellationToken ct = default);
    Task<DotDATNItem> LayDotAsync(long id, CancellationToken ct = default);
    Task<DotDATNItem> TaoDotAsync(DotDATNRequest request, CancellationToken ct = default);
    Task<DotDATNItem> SuaDotAsync(long id, DotDATNRequest request, CancellationToken ct = default);
    Task<DotDATNItem> DongDotAsync(long id, CancellationToken ct = default);
    Task<DanhSachHoSoKetQua> DanhSachHoSoAsync(long dotId, LocHoSoRequest loc, CancellationToken ct = default);
    Task<IReadOnlyList<SinhVienUngVienItem>> TimSinhVienUngVienAsync(long dotId, int? khoaHoc, string? nganh, string? q, CancellationToken ct = default);
    Task<ThemHoSoKetQua> ThemHoSoAsync(long dotId, IReadOnlyList<HoSoTrongDotRequest> items, CancellationToken ct = default);
    Task<ThemHoSoKetQua> ThemHoSoTheoLocAsync(long dotId, ThemHoSoTheoLocRequest loc, CancellationToken ct = default);
    Task<IReadOnlyList<XetGiaoSnapshotItem>> LaySnapshotAsync(long svTrongDotId, CancellationToken ct = default);
    Task<IReadOnlyList<HoSoCuaToiXetGiaoItem>> LayKetQuaCuaToiAsync(CancellationToken ct = default);
    Task<XetGiaoSnapshotItem> XetLaiAsync(long svTrongDotId, CancellationToken ct = default);
    Task<long> TaoKhieuNaiAsync(long svTrongDotId, DonKhieuNaiRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<KhieuNaiItem>> DanhSachKhieuNaiAsync(long dotId, TrangThaiDonTu? trangThai, CancellationToken ct = default);
    Task XuLyKhieuNaiAsync(long donTuId, XuLyKhieuNaiRequest request, CancellationToken ct = default);
    Task<QuyetDinhGiaoItem> GhiNhanQuyetDinhAsync(long dotId, QuyetDinhGiaoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuyetDinhGiaoItem>> DanhSachQuyetDinhGiaoAsync(long dotId, CancellationToken ct = default);
}

/// <summary>Chạy luật xét giao (G1–G3) cho đợt / một hồ sơ, lưu snapshot và chuyển trạng thái qua DATNWorkflow.</summary>
public interface IXetGiaoBatchRunner
{
    /// <summary>
    /// Xét mọi hồ sơ CHO_XET chưa có snapshot xét giao (chạy lại được – idempotent). Job hệ thống: T01/T02.
    /// </summary>
    Task<XetGiaoTienDo> ChayAsync(long dotId, long? nguoiChayId, IProgress<XetGiaoTienDo>? progress = null, CancellationToken ct = default);

    /// <summary>A19: xét lại một hồ sơ (lưu snapshot XetLai mới, giữ snapshot cũ) do người dùng Phòng ĐT thực hiện.</summary>
    Task<XetGiaoSnapshotItem> XetLaiAsync(long svTrongDotId, long? nguoiChayId, CancellationToken ct = default);
}

/// <summary>Hàng đợi job xét giao (Hangfire) – tách khỏi controller để kiểm tra "đang chạy" và đọc tiến độ.</summary>
public interface IXetGiaoJobScheduler
{
    XetGiaoJobItem DuaVaoHangDoi(long dotId, long? nguoiChayId);
    XetGiaoJobItem? Lay(long dotId, string jobId);
    XetGiaoJobItem? LayGanNhat(long dotId);
}
