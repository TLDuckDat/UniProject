using Upm.Domain.PhanQuyen;

namespace Upm.Application.PhanQuyen;

/// <summary>Vai trò, phạm vi và quyền của người dùng hiện tại (nạp từ DB, cache trong request).</summary>
public sealed record QuyenNguoiDung(
    IReadOnlyList<PhamViVaiTro> VaiTro,
    IReadOnlySet<string> Quyen,
    IReadOnlyList<long> CoSoDuocPhep,
    long? CoSoDangLamViecId)
{
    public static readonly QuyenNguoiDung KhongCo = new([], new HashSet<string>(), [], null);

    public bool CoQuyen(string quyen) => Quyen.Contains(quyen);

    public bool CoVaiTro(params string[] maVaiTro) => VaiTro.Any(v => maVaiTro.Contains(v.MaVaiTro, StringComparer.Ordinal));

    /// <summary>Phạm vi dữ liệu (mọi vai trò hoặc chỉ các vai trò chỉ định), đã giao với cơ sở đang làm việc.</summary>
    public PhamViTruyCap PhamVi(IEnumerable<string>? chiVaiTro = null) =>
        PhamViTruyCap.Tu(VaiTro, CoSoDangLamViecId, chiVaiTro);
}

public interface IQuyenHienTai
{
    Task<QuyenNguoiDung> LayAsync(CancellationToken ct = default);
}

/// <summary>
/// Tự lọc truy vấn danh sách theo phạm vi cơ sở → khoa → bộ môn của người dùng (CLAUDE.md §5.7).
/// Mỗi loại entity cần đăng ký cách xác định (cơ sở, khoa, bộ môn) – loại chưa đăng ký sẽ ném lỗi
/// để không lọt dữ liệu.
/// </summary>
public interface IScopeFilter
{
    Task<IQueryable<T>> LocAsync<T>(IQueryable<T> query, IEnumerable<string>? chiVaiTro = null, CancellationToken ct = default)
        where T : class;
}

/// <summary>Vai trò của người dùng hiện tại trên TỪNG hồ sơ SV (PhanCong, ThanhVienHoiDong) – CLAUDE.md §5.7.</summary>
public interface IQuyenTrenSV
{
    /// <summary>Đang là GVHD (phân công còn hiệu lực).</summary>
    Task<bool> LaGVHDAsync(long svTrongDotId, CancellationToken ct = default);

    /// <summary>Đang là GVĐD (phân công còn hiệu lực).</summary>
    Task<bool> LaGVDDAsync(long svTrongDotId, CancellationToken ct = default);

    /// <summary>Là thành viên Hội đồng có lịch bảo vệ của hồ sơ này.</summary>
    Task<bool> LaThanhVienHoiDongAsync(long svTrongDotId, CancellationToken ct = default);

    /// <summary>Hồ sơ là của chính sinh viên đang đăng nhập.</summary>
    Task<bool> LaChinhSVAsync(long svTrongDotId, CancellationToken ct = default);
}
