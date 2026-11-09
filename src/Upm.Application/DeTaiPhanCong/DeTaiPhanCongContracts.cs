using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Application.DeTaiPhanCong;

// ===================== UC-2 · Đề tài & phân công (đặc tả mục 5, B01–B11) =====================

public sealed record DotTomTat(long Id, long CoSoId, string MaDot, string TenDot, string NamHoc, byte HocKy, string TrangThai);

public sealed record BoMonTomTat(long Id, string MaBM, string TenBM, string TenKhoa);

public sealed record DeTaiTaoRequest(long DotId, long BoMonId, string TenDeTai, string? MoTa, LoaiDeTai LoaiDeTai, byte MaxSV = 1);

/// <summary>SV tự đề xuất (B04): GV được đề nghị phải xác nhận, rồi Bộ môn duyệt.</summary>
public sealed record SvDeXuatRequest(long SVTrongDotId, long GiangVienDeNghiId, string TenDeTai, string? MoTa, LoaiDeTai LoaiDeTai);

public sealed record DeTaiTrungItem(long Id, string TenDeTai, string MaDot, string NamHoc, double DoTuongDong, bool TrungTen);

public sealed record DeTaiItem(
    long Id,
    long DotId,
    string MaDot,
    long BoMonId,
    string MaBoMon,
    string TenBoMon,
    string TenDeTai,
    string? MoTa,
    LoaiDeTai LoaiDeTai,
    byte MaxSV,
    NguonDeXuat NguonDeXuat,
    long? GVDeXuatId,
    string? TenGVDeXuat,
    long? SVDeXuatId,
    string? MaSVDeXuat,
    TrangThaiDuyetDeTai TrangThaiDuyet,
    bool CanhBaoTrung,
    int SoSVDaNhan,
    bool DaDu,
    IReadOnlyList<DeTaiTrungItem> TrungVoi);

public sealed record DuyetDeTaiRequest(bool DongY);

public sealed record NguyenVongRequest(IReadOnlyList<long> DeTaiIds);

public sealed record NguyenVongItem(long Id, long DeTaiId, string TenDeTai, byte ThuTuNguyenVong, TrangThaiNguyenVong TrangThai, DateTime NgayDangKy);

public sealed record DuyetDangKyRequest(long SVTrongDotId, bool DongY);

public sealed record DangKyNguyenVongItem(
    long Id,
    long DeTaiId,
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    byte ThuTuNguyenVong,
    TrangThaiNguyenVong TrangThai,
    DateTime NgayDangKy);

public sealed record KhaiBaoXungDotRequest(long SinhVienId, string LyDo);

public sealed record GiangVienTomTat(long Id, string MaGV, string HoTen, LoaiGiangVien LoaiGV);

/// <summary>Phân công một lần cho hồ sơ (GVHD bắt buộc; GVĐD, đồng HD tùy chọn). DeTaiId gắn đề tài nếu hồ sơ chưa có (B05).</summary>
public sealed record PhanCongRequest(
    long SVTrongDotId,
    long GVHDId,
    long? GVDDId,
    IReadOnlyList<long> DongHDIds,
    long? DeTaiId = null,
    bool VuotQuotaXacNhan = false);

public sealed record PhanCongHoSoItem(
    long SVTrongDotId,
    long SinhVienId,
    string MaSV,
    string HoTen,
    string MaCTDT,
    TrangThaiSVTrongDot TrangThai,
    long? DeTaiId,
    string? TenDeTai,
    GiangVienTomTat? GVHD,
    GiangVienTomTat? GVDD,
    IReadOnlyList<GiangVienTomTat> DongHD,
    string RowVersion);

public sealed record TaiGiangVienItem(
    long GiangVienId,
    string MaGV,
    string HoTen,
    LoaiGiangVien LoaiGV,
    int DangCo,
    int Quota,
    MucTaiGiangVien MucTai);

public sealed record PhanCongDotItem(IReadOnlyList<PhanCongHoSoItem> HoSo, IReadOnlyList<TaiGiangVienItem> Tai);

/// <summary>Gợi ý (B05) – chỉ đề xuất, người duyệt phải bấm xác nhận bằng <see cref="PhanCongRequest"/>.</summary>
public sealed record GoiYPhanCongItem(
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    long? GoiYDeTaiId,
    string? TenDeTai,
    long? GoiYGVHDId,
    string? MaGVHD,
    string? HoTenGVHD);

public sealed record PhanCongLichSuItem(
    long Id,
    VaiTroPhanCong VaiTro,
    string MaGV,
    string HoTen,
    DateOnly NgayBatDau,
    DateOnly? NgayKetThuc,
    string? LyDoThayDoi,
    string? SoQuyetDinh,
    bool VuotQuotaDuyetBoi);

/// <summary>Đơn SV xin đổi GVHD (B08) hoặc đổi tên/nội dung đề tài (B09) – sau khi có QĐ giao.</summary>
public sealed record DonDieuChinhTaoRequest(LoaiDon LoaiDon, string NoiDung);

public sealed record DonDieuChinhItem(
    long DonTuId,
    LoaiDon LoaiDon,
    long SVTrongDotId,
    string MaSV,
    string HoTen,
    string NoiDung,
    DateTime NgayGui,
    TrangThaiDonTu TrangThai,
    string? KetQuaXuLy,
    long? DeTaiId,
    string? TenDeTai,
    string? GVHDHienTai);

/// <summary>Trưởng BM duyệt đơn điều chỉnh: ghi QĐ điều chỉnh (DieuChinh), đổi GVHD hoặc đổi tên đề tài.</summary>
public sealed record DuyetDonDieuChinhRequest(
    bool DongY,
    string? LyDo,
    string? SoQuyetDinh,
    DateOnly? NgayKy,
    long? GiangVienMoiId,
    string? TenDeTaiMoi,
    string? MoTaMoi);

public interface IDeTaiService
{
    Task<IReadOnlyList<DeTaiItem>> DanhSachAsync(long dotId, long? boMonId, TrangThaiDuyetDeTai? trangThai, string? q, CancellationToken ct = default);
    Task<IReadOnlyList<DeTaiItem>> DeTaiCuaToiAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<GiangVienTomTat>> DanhSachGvDeNghiAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<BoMonTomTat>> DanhSachBoMonAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<DotTomTat>> DanhSachDotAsync(CancellationToken ct = default);
    Task<DeTaiItem> GvDeXuatAsync(DeTaiTaoRequest request, CancellationToken ct = default);
    Task<DeTaiItem> SvDeXuatAsync(SvDeXuatRequest request, CancellationToken ct = default);
    Task<DeTaiItem> GvXacNhanSvDeXuatAsync(long deTaiId, bool dongY, CancellationToken ct = default);
    Task<DeTaiItem> BoMonDuyetAsync(long deTaiId, bool dongY, CancellationToken ct = default);
    Task<IReadOnlyList<NguyenVongItem>> DangKyNguyenVongAsync(long svTrongDotId, NguyenVongRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<NguyenVongItem>> LayNguyenVongAsync(long svTrongDotId, CancellationToken ct = default);
    Task<IReadOnlyList<DangKyNguyenVongItem>> DanhSachDangKyAsync(long deTaiId, CancellationToken ct = default);
    Task<DangKyNguyenVongItem> GvDuyetDangKyAsync(long deTaiId, DuyetDangKyRequest request, CancellationToken ct = default);
    Task<long> KhaiBaoXungDotAsync(KhaiBaoXungDotRequest request, CancellationToken ct = default);
    Task<PhanCongHoSoItem> PhanCongAsync(PhanCongRequest request, CancellationToken ct = default);
    Task<PhanCongDotItem> PhanCongDotAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<GoiYPhanCongItem>> GoiYPhanCongAsync(long dotId, CancellationToken ct = default);
    Task<IReadOnlyList<PhanCongLichSuItem>> LichSuPhanCongAsync(long svTrongDotId, CancellationToken ct = default);
    Task<long> TaoDonDieuChinhAsync(long svTrongDotId, DonDieuChinhTaoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DonDieuChinhItem>> DanhSachDonDieuChinhAsync(long dotId, CancellationToken ct = default);
    Task DuyetDonDieuChinhAsync(long donTuId, DuyetDonDieuChinhRequest request, CancellationToken ct = default);
}
