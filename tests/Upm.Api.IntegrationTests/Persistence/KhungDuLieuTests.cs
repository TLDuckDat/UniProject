using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.PhanQuyen;
using Upm.Domain.PhanQuyen;
using Upm.Infrastructure.Identity;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.Persistence;

/// <summary>Phần 3b – B (khung dữ liệu): cột audit, khởi động trên DB sạch, IScopeFilter đi qua navigation.</summary>
[Collection(SharedApiFixture.Name)]
public sealed class KhungDuLieuTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] BangKhongAudit =
        ["AuditLog", "LichSuTrangThai", "RefreshToken", "BangDiem", "KyLuatSV", "ThongTinTienDoSV"];

    [Fact]
    public async Task Cot_audit_co_o_moi_bang_nghiep_vu_tru_6_bang_ngoai_le()
    {
        var cot = await QueryAsync(
            factory.ConnectionString,
            "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND COLUMN_NAME IN ('NgayTao','NguoiTaoId','NgaySua','NguoiSuaId')",
            r => (Bang: r.GetString(0), Cot: r.GetString(1), Kieu: r.GetString(2), Null: r.GetString(3)));
        var tatCaBang = await QueryAsync(
            factory.ConnectionString,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'",
            r => r.GetString(0));

        var coAudit = tatCaBang.Except(BangKhongAudit).ToList();
        Assert.Equal(41, coAudit.Count);
        foreach (var bang in coAudit)
        {
            var c = cot.Where(x => x.Bang == bang).ToDictionary(x => x.Cot);
            Assert.Equal(("datetime2", "NO"), (c["NgayTao"].Kieu, c["NgayTao"].Null));
            Assert.Equal(("bigint", "YES"), (c["NguoiTaoId"].Kieu, c["NguoiTaoId"].Null));
            Assert.Equal(("datetime2", "YES"), (c["NgaySua"].Kieu, c["NgaySua"].Null));
            Assert.Equal(("bigint", "YES"), (c["NguoiSuaId"].Kieu, c["NguoiSuaId"].Null));
        }
        Assert.DoesNotContain(cot, x => BangKhongAudit.Contains(x.Bang));

        // NguoiTaoId / NguoiSuaId là FK tới TaiKhoan, không xóa dây chuyền.
        var fk = await QueryAsync(
            factory.ConnectionString,
            """
            SELECT COUNT(*) FROM sys.foreign_key_columns fkc
            JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            JOIN sys.foreign_keys f ON f.object_id = fkc.constraint_object_id
            WHERE c.name IN ('NguoiTaoId','NguoiSuaId') AND OBJECT_NAME(fkc.referenced_object_id) = 'TaiKhoan'
              AND f.delete_referential_action = 0
            """,
            r => r.GetInt32(0));
        Assert.Equal(41 * 2, fk.Single());
    }

    [Fact]
    public async Task Khoi_dong_tren_database_sach_ap_du_migration_Hangfire_va_health_xanh()
    {
        var tenDb = $"UpmSach_{Guid.NewGuid():N}";
        var cs = factory.ConnectionStringFor(tenDb);
        await using var app = factory.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Upm", cs));

        var health = await app.CreateClient().GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("Healthy", await health.Content.ReadAsStringAsync(Ct));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync(Ct));
            Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
            Assert.Equal(2, await db.CoSo.CountAsync(Ct)); // HasData
        }

        var hangfire = await QueryAsync(cs,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'HangFire'", r => r.GetString(0));
        Assert.Contains("Job", hangfire);
        Assert.Contains("Server", hangfire);
        Assert.Contains("Schema", hangfire);
    }

    [Fact]
    public async Task Moi_loai_entity_khai_bao_trong_ScopeFilter_dich_duoc_sang_SQL_va_chay_duoc()
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var khoa = await db.Khoa.FirstAsync(Ct);
        var bm = await db.BoMon.FirstAsync(Ct);
        var quyen = new QuyenCoDinh(new QuyenNguoiDung(
            [
                new PhamViVaiTro(VaiTroMa.GiangVien, khoa.CoSoId, khoa.Id, null),
                new PhamViVaiTro(VaiTroMa.TruongBoMon, khoa.CoSoId, bm.KhoaId, bm.Id),
            ],
            new HashSet<string>(), [khoa.CoSoId], khoa.CoSoId));
        var filter = new ScopeFilter(quyen);

        var method = typeof(KhungDuLieuTests).GetMethod(nameof(ChayLocAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        foreach (var loai in ScopeFilter.LoaiDaKhaiBao)
        {
            await (Task)method.MakeGenericMethod(loai).Invoke(null, [db, filter])!;
        }
        Assert.True(ScopeFilter.LoaiDaKhaiBao.Count >= 20);
    }

    private static async Task ChayLocAsync<T>(UpmDbContext db, ScopeFilter filter) where T : class
    {
        var q = await filter.LocAsync(db.Set<T>().AsNoTracking(), ct: Ct);
        Assert.Contains("WHERE", q.ToQueryString(), StringComparison.Ordinal);
        await q.CountAsync(Ct); // chạy thật trên SQL Server
    }

    private sealed class QuyenCoDinh(QuyenNguoiDung q) : IQuyenHienTai
    {
        public Task<QuyenNguoiDung> LayAsync(CancellationToken ct = default) => Task.FromResult(q);
    }

    private static async Task<List<T>> QueryAsync<T>(string cs, string sql, Func<SqlDataReader, T> map)
    {
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync(Ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(Ct);
        var result = new List<T>();
        while (await reader.ReadAsync(Ct))
        {
            result.Add(map(reader));
        }
        return result;
    }
}
