namespace Upm.Application.Abstractions;

/// <summary>
/// Thông tin bổ sung cho AuditLog trong phạm vi một request (scoped).
/// Use case sửa dữ liệu đã khóa (sau chốt) phải đặt <see cref="SoQDBatBuoc"/> trước khi SaveChanges (CLAUDE.md §5.6).
/// </summary>
public interface IAuditContext
{
    string? SoQDBatBuoc { get; set; }
}

public sealed class AuditContext : IAuditContext
{
    public string? SoQDBatBuoc { get; set; }
}
