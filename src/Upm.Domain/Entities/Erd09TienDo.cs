// Tệp đính kèm và thông báo trong ứng dụng (UC-3a/3b). Không có trong thiet-ke.md – bổ sung khi làm Phần 9.
using Upm.Domain.Common;

namespace Upm.Domain.Entities;

/// <summary>Metadata file lưu qua <c>IFileStorage</c>; <c>FileId</c> ở các bảng nghiệp vụ trỏ vào đây.</summary>
public class TepDinhKem : AuditableEntity
{
    public string TenGoc { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long KichThuocByte { get; set; }
    /// <summary>Khóa lưu trữ tương đối (do <c>IFileStorage</c> cấp).</summary>
    public string DuongDan { get; set; } = string.Empty;
    /// <summary>SHA-256 hex (64 ký tự) của nội dung file.</summary>
    public string HashSHA256 { get; set; } = string.Empty;
}

/// <summary>Thông báo trong ứng dụng (chuông thông báo).</summary>
public class ThongBao : AuditableEntity
{
    public long TaiKhoanId { get; set; }
    public TaiKhoan TaiKhoan { get; set; } = null!;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public bool DaDoc { get; set; }
}
