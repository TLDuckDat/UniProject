using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Upm.Application.Abstractions;
using Upm.Domain.Common;

namespace Upm.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Tự điền cột audit cho entity <see cref="IAuditable"/>: thêm → NgayTao/NguoiTaoId; sửa → NgaySua/NguoiSuaId.
/// Người lấy từ <see cref="ICurrentUser"/> (job hệ thống/seeder → NULL). Không cho sửa NgayTao/NguoiTaoId sau khi tạo.
/// </summary>
public sealed class AuditFieldsInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    /// <summary>Tên các cột audit – AuditLogInterceptor bỏ qua chúng khi ghi giá trị cũ/mới.</summary>
    public static readonly IReadOnlySet<string> CotAudit = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(IAuditable.NgayTao), nameof(IAuditable.NguoiTaoId), nameof(IAuditable.NgaySua), nameof(IAuditable.NguoiSuaId),
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.Now.UtcDateTime;
        var nguoi = currentUser.TaiKhoanId;
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.NgayTao = now;
                    entry.Entity.NguoiTaoId = nguoi;
                    entry.Entity.NgaySua = null;
                    entry.Entity.NguoiSuaId = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.NgaySua = now;
                    entry.Entity.NguoiSuaId = nguoi;
                    // Thông tin tạo là bất biến.
                    entry.Property(x => x.NgayTao).IsModified = false;
                    entry.Property(x => x.NguoiTaoId).IsModified = false;
                    break;
            }
        }
    }
}
