namespace Upm.Domain.Common;

/// <summary>Lớp gốc cho mọi entity: khóa chính <c>Id bigint</c> (IDENTITY).</summary>
public abstract class Entity
{
    public long Id { get; set; }
}

/// <summary>
/// Cột audit (quyết định BA, Phần 3b): hệ thống tự điền qua AuditFieldsInterceptor – KHÔNG gán tay.
/// Người = TaiKhoan.Id; NULL khi job hệ thống / seeder thao tác.
/// </summary>
public interface IAuditable
{
    /// <summary>UTC.</summary>
    DateTime NgayTao { get; set; }
    long? NguoiTaoId { get; set; }
    /// <summary>UTC; NULL nếu chưa sửa lần nào.</summary>
    DateTime? NgaySua { get; set; }
    long? NguoiSuaId { get; set; }
}

/// <summary>
/// Entity nghiệp vụ có cột audit. Ngoại lệ (kế thừa <see cref="Entity"/>): AuditLog, LichSuTrangThai,
/// RefreshToken và bảng đồng bộ đã có DongBoLuc (BangDiem, KyLuatSV, ThongTinTienDoSV).
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    public DateTime NgayTao { get; set; }
    public long? NguoiTaoId { get; set; }
    public DateTime? NgaySua { get; set; }
    public long? NguoiSuaId { get; set; }
}
