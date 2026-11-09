// ERD-02 · Cấu hình & dữ liệu đồng bộ (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

/// <summary>Phiên bản quy chế (UTC-2021, UTC-2026…). Gắn theo KHÓA sinh viên qua RuleSetKhoaHoc.</summary>
public class RuleSet : AuditableEntity
{
    public string MaRuleSet { get; set; } = string.Empty;
    public string TenRuleSet { get; set; } = string.Empty;
    public string? CanCuPhapLy { get; set; }
    public int MaxTCChuaTichLuy { get; set; }
    public decimal GpaBaoVeMin { get; set; }
    public decimal GpaTotNghiepMin { get; set; }
    public decimal DiemDatDATN { get; set; }
    /// <summary>⚠O1: trọng số quá trình mặc định (Bộ môn ghi đè ở ThamSoBoMon).</summary>
    public decimal TrongSoQT { get; set; }
    public decimal HeSoThoiGianToiDa { get; set; }
    public int MinTVHoiDong { get; set; }
    public int MaxTVHoiDong { get; set; }
    /// <summary>⚠O6.</summary>
    public decimal NguongHaHangHocLaiPct { get; set; }
    public bool HaHangKhiKyLuat { get; set; }
    public DateOnly HieuLucTu { get; set; }
    public DateOnly? HieuLucDen { get; set; }
}

public class RuleSetKhoaHoc : AuditableEntity
{
    public long RuleSetId { get; set; }
    public RuleSet RuleSet { get; set; } = null!;
    public int KhoaHocTu { get; set; }
    /// <summary>NULL = không giới hạn trên (vd K67 trở đi).</summary>
    public int? KhoaHocDen { get; set; }
    /// <summary>⚠O2: K66 tạm gán UTC-2021, cần xác nhận.</summary>
    public bool CanXacNhan { get; set; }
}

public class ThamSoBoMon : AuditableEntity
{
    public long BoMonId { get; set; }
    public BoMon BoMon { get; set; } = null!;
    public long RuleSetId { get; set; }
    public RuleSet RuleSet { get; set; } = null!;
    public decimal TrongSoQT { get; set; }
    public decimal TrongSoHD { get; set; }
    public bool TachDiemKhac { get; set; }
    /// <summary>1–3 ngày.</summary>
    public byte SoNgaySuaGapToiDa { get; set; }
    /// <summary>⚠O4.</summary>
    public bool DungHanKhiNghiTamThoi { get; set; }
}

public class ThamSoDaoVan : AuditableEntity
{
    public long KhoaId { get; set; }
    public Khoa Khoa { get; set; } = null!;
    public long? RuleSetId { get; set; }
    public RuleSet? RuleSet { get; set; }
    public LoaiChuongTrinh ChuongTrinh { get; set; }
    public bool Bat { get; set; }
    /// <summary>20–25%.</summary>
    public decimal NguongPhanTram { get; set; }
}

public class HocPhan : AuditableEntity
{
    public string MaHP { get; set; } = string.Empty;
    public string TenHP { get; set; } = string.Empty;
    public int SoTC { get; set; }
    public LoaiHocPhan LoaiHP { get; set; }
    public bool TinhDiem { get; set; } = true;
}

public class CTDTHocPhan : AuditableEntity
{
    public long CTDTId { get; set; }
    public ChuongTrinhDaoTao CTDT { get; set; } = null!;
    public long HocPhanId { get; set; }
    public HocPhan HocPhan { get; set; } = null!;
    public int HocKyKeHoach { get; set; }
    public bool LaHPThayTheDATN { get; set; }
}

/// <summary>Đồng bộ từ QLĐT (1:1 với SinhVien). Không có cột audit – đã có DongBoLuc.</summary>
public class ThongTinTienDoSV : Entity
{
    public long SinhVienId { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public string KyCuoiKeHoach { get; set; } = string.Empty;
    /// <summary>2 × thời gian chuẩn.</summary>
    public DateOnly HanToiDa { get; set; }
    public bool DungTienDo { get; set; }
    public DateTime DongBoLuc { get; set; }
    public string? NguonDongBo { get; set; }
}

/// <summary>Đồng bộ từ QLĐT – hệ thống ĐATN chỉ đọc. Không có cột audit – đã có DongBoLuc.</summary>
public class BangDiem : Entity
{
    public long SinhVienId { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public long HocPhanId { get; set; }
    public HocPhan HocPhan { get; set; } = null!;
    public string HocKy { get; set; } = string.Empty;
    public int LanHoc { get; set; } = 1;
    public decimal? Diem10 { get; set; }
    public string? DiemChu { get; set; }
    public decimal? Diem4 { get; set; }
    public TrangThaiDiem TrangThaiDiem { get; set; }
    public string NguonDongBo { get; set; } = string.Empty;
    public DateTime DongBoLuc { get; set; }
}

/// <summary>Đồng bộ từ CTSV. Không có cột audit – nguồn đồng bộ ghi ở NguonCTSV.</summary>
public class KyLuatSV : Entity
{
    public long SinhVienId { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public string MucKyLuat { get; set; } = string.Empty;
    public DateOnly TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public bool DinhChiHocTap { get; set; }
    public bool TruyCuuHinhSu { get; set; }
    public string NguonCTSV { get; set; } = string.Empty;
    public string? NguonDongBo { get; set; }
    public DateTime? DongBoLuc { get; set; }
}
