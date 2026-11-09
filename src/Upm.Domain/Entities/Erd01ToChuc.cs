// ERD-01 · Tổ chức & nhân sự (docs/thiet-ke.md). Tên lớp/thuộc tính = tên bảng/cột.
// Navigation tham chiếu (many-to-one) cho mọi FK; KHÔNG bật lazy loading – dùng Include/projection khi cần.
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

public class CoSo : AuditableEntity
{
    public string MaCoSo { get; set; } = string.Empty;
    public string TenCoSo { get; set; } = string.Empty;
    public bool ConHoatDong { get; set; } = true;
}

public class Khoa : AuditableEntity
{
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public string MaKhoa { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public KhoiNganh KhoiNganh { get; set; }
}

public class BoMon : AuditableEntity
{
    public long KhoaId { get; set; }
    public Khoa Khoa { get; set; } = null!;
    public string MaBM { get; set; } = string.Empty;
    public string TenBM { get; set; } = string.Empty;
}

public class GiangVien : AuditableEntity
{
    public long BoMonId { get; set; }
    public BoMon BoMon { get; set; } = null!;
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public string MaGV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? HocHam { get; set; }
    public string? HocVi { get; set; }
    public LoaiGiangVien LoaiGV { get; set; }
    /// <summary>Số SV tối đa / đợt (5–10).</summary>
    public int QuotaToiDa { get; set; }
    public string? Email { get; set; }
    public bool ConHoatDong { get; set; } = true;
}

public class SinhVien : AuditableEntity
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    /// <summary>Khóa tuyển sinh, vd 64, 65, 67 → chọn RuleSet qua RuleSetKhoaHoc.</summary>
    public int KhoaHoc { get; set; }
    public long CTDTId { get; set; }
    public ChuongTrinhDaoTao CTDT { get; set; } = null!;
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public TrangThaiHocTap TrangThaiHocTap { get; set; }
    public DateOnly NgayNhapHoc { get; set; }
    public string? Email { get; set; }
    /// <summary>Nguồn cập nhật gần nhất (QLĐT import/API).</summary>
    public string? NguonDongBo { get; set; }
    /// <summary>Thời điểm đồng bộ gần nhất UTC.</summary>
    public DateTime? DongBoLuc { get; set; }
}

public class ChuongTrinhDaoTao : AuditableEntity
{
    public long KhoaId { get; set; }
    public Khoa Khoa { get; set; } = null!;
    public string MaCTDT { get; set; } = string.Empty;
    public string TenCTDT { get; set; } = string.Empty;
    public string Nganh { get; set; } = string.Empty;
    public HeDaoTao He { get; set; }
    public LoaiChuongTrinh ChuongTrinh { get; set; }
    public int TongTCYeuCau { get; set; }
    /// <summary>Số TC ĐATN (7–12).</summary>
    public int TCDATN { get; set; }
    public int TCTTTN { get; set; }
    public bool CoHocPhanThayThe { get; set; }
}

public class TaiKhoan : AuditableEntity
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long? GiangVienId { get; set; }
    public GiangVien? GiangVien { get; set; }
    public long? SinhVienId { get; set; }
    public SinhVien? SinhVien { get; set; }
    public LoaiTaiKhoan LoaiTK { get; set; }
    public bool ConHoatDong { get; set; } = true;
    /// <summary>
    /// [Bổ sung Phần 3 – không có trong thiet-ke.md] Băm bằng PasswordHasher của ASP.NET Identity.
    /// NULL = tài khoản không đăng nhập bằng mật khẩu (vd. HeThongNgoai) hoặc chưa đặt mật khẩu.
    /// </summary>
    public string? MatKhauHash { get; set; }
}

public class VaiTro : AuditableEntity
{
    public string MaVaiTro { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}

/// <summary>Vai trò hệ thống + phạm vi (CoSoId/KhoaId/BoMonId; NULL = phạm vi rộng hơn).</summary>
public class TaiKhoanVaiTro : AuditableEntity
{
    public long TaiKhoanId { get; set; }
    public TaiKhoan TaiKhoan { get; set; } = null!;
    public long VaiTroId { get; set; }
    public VaiTro VaiTro { get; set; } = null!;
    public long? CoSoId { get; set; }
    public CoSo? CoSo { get; set; }
    public long? KhoaId { get; set; }
    public Khoa? Khoa { get; set; }
    public long? BoMonId { get; set; }
    public BoMon? BoMon { get; set; }
}
