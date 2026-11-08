using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Upm.Application.Abstractions;
using Upm.Domain.Entities;
using Upm.Domain.Enums;

namespace Upm.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Ghi AuditLog cho mọi INSERT/UPDATE/DELETE qua EF (CLAUDE.md §5.3, §5.6):
/// bảng, Id, giá trị cũ/mới (JSON – UPDATE chỉ các cột đổi), người dùng, IP, CoSoId (nếu entity có), SoQDBatBuoc.
/// <para>
/// Log được ghi SAU khi lưu (để INSERT có Id) nhưng trong CÙNG transaction: nếu chưa có transaction thì interceptor mở,
/// ghi log rồi commit – dữ liệu và log cùng thành công hoặc cùng thất bại.
/// </para>
/// Bỏ qua: AuditLog (chính nó), RefreshToken (nhiễu, có dữ liệu nhạy cảm), LichSuTrangThai (workflow tự ghi AuditLog Transition).
/// Không áp cho ExecuteUpdate/ExecuteDelete (đi thẳng SQL) – use case dùng các lệnh đó phải tự ghi log.
/// </summary>
public sealed class AuditLogInterceptor(ICurrentUser currentUser, IAuditContext auditContext, IClock clock) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> BoQua = [typeof(AuditLog), typeof(RefreshToken), typeof(LichSuTrangThai)];
    /// <summary>
    /// Cột bí mật bị che ("***") trong GiaTriCu/GiaTriMoi: mọi cột mật khẩu, token, khóa bí mật, và cột băm của
    /// chúng (MatKhauHash, TokenHash…). KHÔNG che <c>PhienBanQuyen.HashSHA256</c> – đó là dấu vân tay file quyển
    /// (công khai, cần giữ để đối chiếu bản bảo vệ), không phải bí mật.
    /// </summary>
    public static bool LaCotNhayCam(string tenCot) =>
        tenCot != nameof(PhienBanQuyen.HashSHA256)
        && (tenCot.Contains("MatKhau", StringComparison.OrdinalIgnoreCase)
            || tenCot.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || tenCot.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || tenCot.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || tenCot.EndsWith("Hash", StringComparison.OrdinalIgnoreCase));

    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // giữ tiếng Việt dễ đọc
        Converters = { new JsonStringEnumConverter() },
    };

    private List<ThayDoi>? _choGhi;
    private IDbContextTransaction? _txRieng;
    private bool _dangGhiLog;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (ChuanBi(eventData.Context) && eventData.Context!.Database.CurrentTransaction is null)
        {
            _txRieng = eventData.Context.Database.BeginTransaction();
        }
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (ChuanBi(eventData.Context) && eventData.Context!.Database.CurrentTransaction is null)
        {
            _txRieng = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
        }
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (GhiLog(eventData.Context))
        {
            _dangGhiLog = true;
            try
            {
                eventData.Context!.SaveChanges();
                _txRieng?.Commit();
            }
            finally
            {
                _dangGhiLog = false;
                DonDep();
            }
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (GhiLog(eventData.Context))
        {
            _dangGhiLog = true;
            try
            {
                await eventData.Context!.SaveChangesAsync(cancellationToken);
                if (_txRieng is not null)
                {
                    await _txRieng.CommitAsync(cancellationToken);
                }
            }
            finally
            {
                _dangGhiLog = false;
                DonDep();
            }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (!_dangGhiLog)
        {
            _txRieng?.Rollback();
            DonDep();
        }
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!_dangGhiLog)
        {
            if (_txRieng is not null)
            {
                await _txRieng.RollbackAsync(cancellationToken);
            }
            DonDep();
        }
    }

    /// <summary>Chụp thay đổi TRƯỚC khi lưu (giá trị gốc mất sau khi lưu). Trả true nếu có gì cần log.</summary>
    private bool ChuanBi(DbContext? context)
    {
        if (context is null || _dangGhiLog)
        {
            return false;
        }

        _choGhi = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                        && !BoQua.Contains(e.Entity.GetType()))
            .Select(ChupTruoc)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();

        return _choGhi.Count > 0;
    }

    private static ThayDoi? ChupTruoc(EntityEntry e)
    {
        switch (e.State)
        {
            case EntityState.Added:
                return new ThayDoi(e, HanhDongAudit.Insert, null, null);
            case EntityState.Deleted:
                return new ThayDoi(e, HanhDongAudit.Delete, GiaTri(e, e.Properties, p => p.OriginalValue), null);
            default:
                // Chỉ đổi cột audit (NgaySua/NguoiSuaId) thì không coi là thay đổi nghiệp vụ.
                var doi = e.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)
                                && !AuditFieldsInterceptor.CotAudit.Contains(p.Metadata.Name))
                    .ToList();
                return doi.Count == 0
                    ? null
                    : new ThayDoi(e, HanhDongAudit.Update, GiaTri(e, doi, p => p.OriginalValue), GiaTri(e, doi, p => p.CurrentValue));
        }
    }

    /// <summary>Thêm các dòng AuditLog vào context (sau khi dữ liệu đã lưu). Trả true nếu cần SaveChanges lần 2.</summary>
    private bool GhiLog(DbContext? context)
    {
        if (context is null || _dangGhiLog || _choGhi is not { Count: > 0 })
        {
            if (!_dangGhiLog)
            {
                // Không có gì để log nhưng đã mở transaction (không xảy ra – phòng hờ).
                _txRieng?.Commit();
                DonDep();
            }
            return false;
        }

        var now = clock.Now.UtcDateTime;
        foreach (var t in _choGhi)
        {
            var giaTriMoi = t.HanhDong == HanhDongAudit.Insert
                ? GiaTri(t.Entry, t.Entry.Properties, p => p.CurrentValue)
                : t.GiaTriMoi;

            context.Set<AuditLog>().Add(new AuditLog
            {
                ThoiDiem = now,
                NguoiDungId = currentUser.TaiKhoanId,
                CoSoId = t.Entry.Metadata.FindProperty("CoSoId") is not null ? t.Entry.Property("CoSoId").CurrentValue as long? : null,
                LoaiDoiTuong = t.Entry.Metadata.GetTableName() ?? t.Entry.Entity.GetType().Name,
                DoiTuongId = t.Entry.Metadata.FindProperty("Id") is not null ? t.Entry.Property("Id").CurrentValue as long? : null,
                HanhDong = t.HanhDong,
                GiaTriCu = t.GiaTriCu,
                GiaTriMoi = giaTriMoi,
                SoQDBatBuoc = auditContext.SoQDBatBuoc,
                IpAddress = currentUser.IpAddress,
            });
        }

        _choGhi = null;
        return true;
    }

    private static string GiaTri(EntityEntry e, IEnumerable<PropertyEntry> props, Func<PropertyEntry, object?> chon)
    {
        var dict = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var p in props)
        {
            // Bỏ rowversion và cột audit (đã có ThoiDiem/NguoiDungId trên chính dòng AuditLog).
            if (p.Metadata.ClrType == typeof(byte[]) || AuditFieldsInterceptor.CotAudit.Contains(p.Metadata.Name))
            {
                continue;
            }
            dict[p.Metadata.Name] = LaCotNhayCam(p.Metadata.Name) ? "***" : chon(p);
        }
        return JsonSerializer.Serialize(dict, Json);
    }

    private void DonDep()
    {
        _txRieng?.Dispose();
        _txRieng = null;
        _choGhi = null;
    }

    private sealed record ThayDoi(EntityEntry Entry, HanhDongAudit HanhDong, string? GiaTriCu, string? GiaTriMoi);
}
