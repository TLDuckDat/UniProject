// ERD-07 · Quyết định, đơn từ, log (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

/// <summary>Gắn số QĐ → DaKhoaDuLieu = true → dữ liệu liên quan bị khóa (CLAUDE.md §5.6).</summary>
public class QuyetDinh : AuditableEntity
{
    public long CoSoId { get; set; }
    public CoSo CoSo { get; set; } = null!;
    public string SoQuyetDinh { get; set; } = string.Empty;
    public DateOnly NgayKy { get; set; }
    public LoaiQuyetDinh LoaiQD { get; set; }
    public string? NguoiKy { get; set; }
    public long? FileScanId { get; set; }
    public bool DaKhoaDuLieu { get; set; }
    public string? GhiChu { get; set; }

    public ICollection<QuyetDinhDoiTuong> DoiTuongs { get; set; } = [];
}

/// <summary>QĐ áp cho nhiều đối tượng – mỗi dòng chỉ một cột đối tượng có giá trị.</summary>
public class QuyetDinhDoiTuong : AuditableEntity
{
    public long QuyetDinhId { get; set; }
    public QuyetDinh QuyetDinh { get; set; } = null!;
    public long? SVTrongDotId { get; set; }
    public SVTrongDot? SVTrongDot { get; set; }
    public long? HoiDongId { get; set; }
    public HoiDong? HoiDong { get; set; }
    public long? PhanCongId { get; set; }
    public PhanCong? PhanCong { get; set; }
    public long? DiemDATNId { get; set; }
    public DiemDATN? DiemDATN { get; set; }
}

public class DonTu : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public LoaiDon LoaiDon { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public long? FileMinhChungId { get; set; }
    public DateTime NgayGui { get; set; }
    public TrangThaiDonTu TrangThai { get; set; }
    public long? NguoiXuLyId { get; set; }
    public TaiKhoan? NguoiXuLy { get; set; }
    public string? KetQuaXuLy { get; set; }
    public DateTime? NgayXuLy { get; set; }
    public DateOnly? HanMoi { get; set; }
    public long? QuyetDinhId { get; set; }
    public QuyetDinh? QuyetDinh { get; set; }
}

/// <summary>Nhật ký chuyển trạng thái – không có cột audit (bản thân đã là log).</summary>
public class LichSuTrangThai : Entity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    /// <summary>NULL khi tạo hồ sơ (chưa có trạng thái trước).</summary>
    public TrangThaiSVTrongDot? TuTrangThai { get; set; }
    public TrangThaiSVTrongDot DenTrangThai { get; set; }
    public string? LyDo { get; set; }
    /// <summary>NULL khi job hệ thống chuyển.</summary>
    public long? NguoiThucHienId { get; set; }
    public TaiKhoan? NguoiThucHien { get; set; }
    public long? QuyetDinhId { get; set; }
    public QuyetDinh? QuyetDinh { get; set; }
    public DateTime ThoiDiem { get; set; }
}

/// <summary>Không có cột audit (bản thân là log).</summary>
public class AuditLog : Entity
{
    public DateTime ThoiDiem { get; set; }
    public long? NguoiDungId { get; set; }
    public TaiKhoan? NguoiDung { get; set; }
    public long? CoSoId { get; set; }
    public string LoaiDoiTuong { get; set; } = string.Empty;
    public long? DoiTuongId { get; set; }
    public HanhDongAudit HanhDong { get; set; }
    /// <summary>JSON.</summary>
    public string? GiaTriCu { get; set; }
    /// <summary>JSON.</summary>
    public string? GiaTriMoi { get; set; }
    public string? SoQDBatBuoc { get; set; }
    public string? IpAddress { get; set; }
}

public class LichNhacViec : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public LoaiNhac LoaiNhac { get; set; }
    public DateOnly HanNgay { get; set; }
    public bool DaGui { get; set; }
    public DateTime? GuiLuc { get; set; }
}
