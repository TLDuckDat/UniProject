// ERD-03 · Đợt & xét điều kiện giao (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

public class DotDATN : AuditableEntity
{
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public string MaDot { get; set; } = string.Empty;
    public string TenDot { get; set; } = string.Empty;
    /// <summary>Dạng "2025-2026".</summary>
    public string NamHoc { get; set; } = string.Empty;
    public byte HocKy { get; set; }
    public DateOnly NgayBatDau { get; set; }
    /// <summary>12–13 tuần.</summary>
    public byte SoTuan { get; set; }
    public DateOnly NgayNopQuyenHan { get; set; }
    public TrangThaiDot TrangThai { get; set; }
}

/// <summary>
/// Hồ sơ ĐATN của 1 SV trong 1 đợt (UK: DotId + SinhVienId + CTDTId).
/// <see cref="TrangThai"/> CHỈ đổi qua DATNWorkflow.Transition() (CLAUDE.md §5.3).
/// </summary>
public class SVTrongDot : AuditableEntity
{
    public long DotId { get; set; }
    public DotDATN Dot { get; set; } = null!;
    public long SinhVienId { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public long CTDTId { get; set; }
    public ChuongTrinhDaoTao CTDT { get; set; } = null!;
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public TrangThaiSVTrongDot TrangThai { get; set; } = TrangThaiSVTrongDot.CHO_XET;
    public bool CoNoTinChi { get; set; }
    public bool ChuaQuaTTTN { get; set; }
    public long? DeTaiId { get; set; }
    public DeTai? DeTai { get; set; }
    public long? QuyetDinhGiaoId { get; set; }
    public QuyetDinh? QuyetDinhGiao { get; set; }
    public int LanLamLai { get; set; }
    public long? HoSoTruocId { get; set; }
    public SVTrongDot? HoSoTruoc { get; set; }
    public DateOnly? NgayHanHoanBaoVe { get; set; }
    /// <summary>rowversion – kiểm soát ghi đồng thời.</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<LichSuTrangThai> LichSuTrangThais { get; set; } = [];
    public ICollection<PhanCong> PhanCongs { get; set; } = [];
}

/// <summary>Snapshot mỗi lần xét – không sửa, không xóa.</summary>
public class XetDieuKien : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public LoaiXet LoaiXet { get; set; }
    public long RuleSetId { get; set; }
    public RuleSet RuleSet { get; set; } = null!;
    public string RuleVersion { get; set; } = string.Empty;
    public DateTime ThoiDiemXet { get; set; }
    /// <summary>NULL khi job hệ thống chạy.</summary>
    public long? NguoiChayId { get; set; }
    public TaiKhoan? NguoiChay { get; set; }
    public string KetQua { get; set; } = string.Empty;
    public int TCChuaTichLuy { get; set; }
    public decimal GpaTichLuy { get; set; }
    public bool? K1_KyLuat { get; set; }
    public bool? K2_GVHD { get; set; }
    public bool? K3_GVDD { get; set; }
    public bool? K4_DuTC { get; set; }
    public bool? K5_Gpa { get; set; }
    public bool? K6_TTTN { get; set; }
    public string? LyDo { get; set; }

    public ICollection<XetDieuKienChiTiet> ChiTiets { get; set; } = [];
}

public class XetDieuKienChiTiet : AuditableEntity
{
    public long XetDieuKienId { get; set; }
    public XetDieuKien XetDieuKien { get; set; } = null!;
    public long HocPhanId { get; set; }
    public HocPhan HocPhan { get; set; } = null!;
    public int SoTC { get; set; }
    public LoaiNo LoaiNo { get; set; }
    public bool BiLoaiTru { get; set; }
}
