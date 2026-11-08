// ERD-05 · Quyển, đọc duyệt, đạo văn (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

public class PhienBanQuyen : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public int SoPhienBan { get; set; }
    public LoaiBanQuyen LoaiBan { get; set; }
    public long FileId { get; set; }
    /// <summary>SHA-256 dạng hex (64 ký tự) của file quyển – dùng đối chiếu bản bảo vệ, không phải bí mật.</summary>
    public string HashSHA256 { get; set; } = string.Empty;
    public long KichThuocByte { get; set; }
    public DateTime NgayNop { get; set; }
    public bool NopQuaHan { get; set; }
    public bool LaBanBaoVe { get; set; }
}

public class YKienGVHD : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long GiangVienId { get; set; }
    public GiangVien GiangVien { get; set; } = null!;
    public bool DongY { get; set; }
    /// <summary>Bắt buộc khi DongY = false (kiểm ở tầng nghiệp vụ).</summary>
    public string? LyDoKhongDongY { get; set; }
    public decimal? DiemGVHD { get; set; }
    /// <summary>Chỉ khi ThamSoBoMon.TachDiemKhac.</summary>
    public decimal? DiemKhac { get; set; }
    public DateTime NgayCho { get; set; }
}

public class KetQuaDocDuyet : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long PhienBanQuyenId { get; set; }
    public PhienBanQuyen PhienBanQuyen { get; set; } = null!;
    public long GVDDId { get; set; }
    public GiangVien GVDD { get; set; } = null!;
    /// <summary>1 hoặc 2 (tối đa 1 vòng sửa gấp).</summary>
    public byte VongDoc { get; set; } = 1;
    public KetQuaDocDuyetLoai KetQua { get; set; }
    public string? NhanXet { get; set; }
    public DateOnly? HanSuaDen { get; set; }
    public decimal? DiemGVDD { get; set; }
    public DateTime? NgayQuyet { get; set; }
}

public class KetQuaKiemTraDaoVan : AuditableEntity
{
    public long PhienBanQuyenId { get; set; }
    public PhienBanQuyen PhienBanQuyen { get; set; } = null!;
    public string PhanMemQuet { get; set; } = string.Empty;
    public decimal TyLeTrungPct { get; set; }
    public decimal NguongApDungPct { get; set; }
    public bool VuotNguong { get; set; }
    public long? FileBaoCaoId { get; set; }
    public DateTime NgayQuet { get; set; }
}

public class LuuTruThuVien : AuditableEntity
{
    public long PhienBanQuyenId { get; set; }
    public PhienBanQuyen PhienBanQuyen { get; set; } = null!;
    public long NguoiNhanId { get; set; }
    public TaiKhoan NguoiNhan { get; set; } = null!;
    public DateTime NgayNhan { get; set; }
    public string MaLuuTru { get; set; } = string.Empty;
    public bool GVHDXacNhan { get; set; }
    public DateTime? NgayGVHDXacNhan { get; set; }
    public string? GhiChu { get; set; }
}
