// ERD-06 · Hội đồng, bảo vệ, điểm (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

public class HoiDong : AuditableEntity
{
    public long DotId { get; set; }
    public DotDATN Dot { get; set; } = null!;
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public long KhoaId { get; set; }
    public Khoa Khoa { get; set; } = null!;
    public string MaHD { get; set; } = string.Empty;
    public string TenHD { get; set; } = string.Empty;
    public HinhThucBaoVe HinhThuc { get; set; }
    public string? LinkGhiHinh { get; set; }
    public long? QuyetDinhId { get; set; }
    public QuyetDinh? QuyetDinh { get; set; }
    public TrangThaiHoiDong TrangThai { get; set; }

    public ICollection<ThanhVienHoiDong> ThanhViens { get; set; } = [];
}

public class ThanhVienHoiDong : AuditableEntity
{
    public long HoiDongId { get; set; }
    public HoiDong HoiDong { get; set; } = null!;
    public long GiangVienId { get; set; }
    public GiangVien GiangVien { get; set; } = null!;
    public ChucDanhHoiDong ChucDanh { get; set; }
    public bool CoMat { get; set; }
    public bool? DongYTrucTuyen { get; set; }
    /// <summary>Thành viên (ThanhVienHoiDong.Id) mà dòng này thay thế.</summary>
    public long? ThayTheChoId { get; set; }
    public ThanhVienHoiDong? ThayTheCho { get; set; }
    public string? LyDoThayThe { get; set; }
}

public class PhongBaoVe : AuditableEntity
{
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public string TenPhong { get; set; } = string.Empty;
    public int SucChua { get; set; }
    public bool ConSuDung { get; set; } = true;
}

public class LichBaoVe : AuditableEntity
{
    public long HoiDongId { get; set; }
    public HoiDong HoiDong { get; set; } = null!;
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long PhongBaoVeId { get; set; }
    public PhongBaoVe PhongBaoVe { get; set; } = null!;
    public DateOnly NgayBaoVe { get; set; }
    public byte Ca { get; set; }
    public int ThuTu { get; set; }
    public TrangThaiLichBaoVe TrangThai { get; set; }
    public decimal TruDiemViPhamPct { get; set; }
    public string? GhiChu { get; set; }

    public ICollection<PhieuDiem> PhieuDiems { get; set; } = [];
}

public class PhieuDiem : AuditableEntity
{
    public long LichBaoVeId { get; set; }
    public LichBaoVe LichBaoVe { get; set; } = null!;
    public long ThanhVienHoiDongId { get; set; }
    public ThanhVienHoiDong ThanhVienHoiDong { get; set; } = null!;
    public decimal Diem { get; set; }
    public string? NhanXet { get; set; }
    /// <summary>Thư ký nhập hộ.</summary>
    public bool NhapHo { get; set; }
    public long NguoiNhapId { get; set; }
    public TaiKhoan NguoiNhap { get; set; } = null!;
    public DateTime NgayNhap { get; set; }
    public bool DaNop { get; set; }
}

public class BienBan : AuditableEntity
{
    public long LichBaoVeId { get; set; }
    public LichBaoVe LichBaoVe { get; set; } = null!;
    /// <summary>ThanhVienHoiDong.Id của thư ký.</summary>
    public long ThuKyId { get; set; }
    public ThanhVienHoiDong ThuKy { get; set; } = null!;
    public string NoiDung { get; set; } = string.Empty;
    public long? FileId { get; set; }
    public DateTime NgayLap { get; set; }
    public decimal ChenhLechMax { get; set; }
    /// <summary>⚠O9: Chủ tịch xác nhận khi chênh lệch ≥ 2,0.</summary>
    public bool ChuTichXacNhan { get; set; }
    public DateTime? XacNhanLuc { get; set; }
}

/// <summary>
/// Điểm ĐATN chốt của một hồ sơ (1:1 với SVTrongDot). Đạt khi ≥ 5,5 (CLAUDE.md §5.5).
/// Cột <c>DiemDATN</c> map vào thuộc tính <see cref="DiemTong"/> vì C# không cho thuộc tính trùng tên lớp.
/// </summary>
public class DiemDATN : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long RuleSetId { get; set; }
    public RuleSet RuleSet { get; set; } = null!;
    public string RuleVersion { get; set; } = string.Empty;
    public decimal DiemGVHD { get; set; }
    public decimal DiemGVDD { get; set; }
    public decimal? DiemKhac { get; set; }
    public decimal DiemQuaTrinh { get; set; }
    public decimal DiemBaoVe { get; set; }
    public decimal TrongSoQT { get; set; }
    public decimal TrongSoHD { get; set; }
    /// <summary>Cột <c>DiemDATN</c>.</summary>
    public decimal DiemTong { get; set; }
    public string DiemChu { get; set; } = string.Empty;
    public decimal DiemHe4 { get; set; }
    public bool Dat { get; set; }
    public DateTime? ChotLuc { get; set; }
    public long? ChotBoiId { get; set; }
    public TaiKhoan? ChotBoi { get; set; }
    public long? QuyetDinhSuaDiemId { get; set; }
    public QuyetDinh? QuyetDinhSuaDiem { get; set; }
}
