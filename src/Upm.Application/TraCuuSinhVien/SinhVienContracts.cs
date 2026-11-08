using Upm.Application.Common;

namespace Upm.Application.TraCuuSinhVien;

public sealed record SinhVienListItem(
    long Id,
    string MaSV,
    string HoTen,
    int KhoaHoc,
    string MaCoSo,
    string TenKhoa,
    string MaCTDT,
    string TrangThaiHocTap);

/// <summary>Tra cứu sinh viên – luôn lọc theo phạm vi người dùng.</summary>
public interface ISinhVienQueries
{
    Task<PagedResult<SinhVienListItem>> DanhSachAsync(string? tuKhoa, PageRequest page, CancellationToken ct = default);
}
