using Upm.Application.Common;

namespace Upm.Application.DanhMuc;

public sealed record DanhMucItem(
    long Id,
    string Ma,
    string Ten,
    long? ParentId = null,
    string? ParentTen = null,
    long? CoSoId = null,
    string? Email = null,
    string? Loai = null,
    int? SoLuong = null,
    int? SoTinChi = null,
    string? Nganh = null,
    bool? ConHoatDong = null,
    bool? TinhDiem = null,
    int? HocKyKeHoach = null,
    bool? LaHPThayTheDATN = null,
    string? He = null,
    string? ChuongTrinh = null,
    int? TongTCYeuCau = null,
    int? TCDATN = null,
    int? TCTTTN = null,
    bool? CoHocPhanThayThe = null,
    long? HocPhanId = null);

public sealed record DanhMucRequest(
    string Ma,
    string Ten,
    long? ParentId = null,
    long? CoSoId = null,
    string? Email = null,
    string? Loai = null,
    int? SoLuong = null,
    int? SoTinChi = null,
    string? Nganh = null,
    bool? ConHoatDong = null,
    bool? TinhDiem = null,
    int? HocKyKeHoach = null,
    bool? LaHPThayTheDATN = null,
    string? He = null,
    string? ChuongTrinh = null,
    int? TongTCYeuCau = null,
    int? TCDATN = null,
    int? TCTTTN = null,
    bool? CoHocPhanThayThe = null,
    long? HocPhanId = null);

public sealed record DanhMucQuery(string? Q, int Page = 1, int PageSize = 20, string? Sort = null, bool Desc = false);

public interface IDanhMucService
{
    Task<PagedResult<DanhMucItem>> DanhSachAsync(string loai, DanhMucQuery query, CancellationToken ct = default);
    Task<DanhMucItem> LayAsync(string loai, long id, CancellationToken ct = default);
    Task<DanhMucItem> TaoAsync(string loai, DanhMucRequest request, CancellationToken ct = default);
    Task<DanhMucItem> SuaAsync(string loai, long id, DanhMucRequest request, CancellationToken ct = default);
    Task XoaAsync(string loai, long id, CancellationToken ct = default);
}

public sealed record TaiKhoanVaiTroRequest(string MaVaiTro, long? CoSoId = null, long? KhoaId = null, long? BoMonId = null);
public sealed record TaiKhoanRequest(string TenDangNhap, string? Email, string? MatKhau, string LoaiTK, bool ConHoatDong,
    long? GiangVienId, long? SinhVienId, IReadOnlyList<TaiKhoanVaiTroRequest> VaiTro);
public sealed record TaiKhoanItem(long Id, string TenDangNhap, string? Email, string LoaiTK, bool ConHoatDong,
    long? GiangVienId, long? SinhVienId, IReadOnlyList<TaiKhoanVaiTroRequest> VaiTro);

public interface ITaiKhoanQuanLyService
{
    Task<PagedResult<TaiKhoanItem>> DanhSachAsync(string? q, PageRequest page, CancellationToken ct = default);
    Task<TaiKhoanItem> TaoAsync(TaiKhoanRequest request, CancellationToken ct = default);
    Task<TaiKhoanItem> SuaAsync(long id, TaiKhoanRequest request, CancellationToken ct = default);
    Task XoaAsync(long id, CancellationToken ct = default);
}
