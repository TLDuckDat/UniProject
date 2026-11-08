using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Domain.Entities;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.Persistence;

/// <summary>Cột audit NgayTao/NguoiTaoId/NgaySua/NguoiSuaId (IAuditable) do AuditFieldsInterceptor tự điền.</summary>
[Collection(SharedApiFixture.Name)]
public sealed class AuditFieldsInterceptorTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Them_dien_NgayTao_NguoiTaoId_sua_dien_NgaySua_NguoiSuaId_va_khong_cho_sua_thong_tin_tao()
    {
        await factory.SeedAsync(Ct);
        long adminId, gvId, hpId;
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            adminId = await db.TaiKhoan.Where(t => t.TenDangNhap == "admin").Select(t => t.Id).SingleAsync(Ct);
            gvId = await db.TaiKhoan.Where(t => t.TenDangNhap == "gv001").Select(t => t.Id).SingleAsync(Ct);
        }

        var lucTao = factory.Clock.Now.UtcDateTime;
        await using (var scope = factory.CreateScope())
        {
            GiaLapNguoiDung(scope, adminId);
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var hp = new HocPhan { MaHP = "UT-AUDITCOL-1", TenHP = "Kiểm thử cột audit", SoTC = 2 };
            db.HocPhan.Add(hp);
            await db.SaveChangesAsync(Ct);
            hpId = hp.Id;
        }

        await using (var scope = factory.CreateScope())
        {
            GiaLapNguoiDung(scope, gvId);
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var hp = await db.HocPhan.SingleAsync(h => h.Id == hpId, Ct);
            Assert.Equal(adminId, hp.NguoiTaoId);
            Assert.InRange(hp.NgayTao, lucTao.AddSeconds(-5), lucTao.AddSeconds(5));
            Assert.Equal(DateTimeKind.Utc, hp.NgayTao.Kind);
            Assert.Null(hp.NgaySua);
            Assert.Null(hp.NguoiSuaId);

            hp.SoTC = 3;
            hp.NgayTao = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc); // cố tình sửa – phải bị chặn
            hp.NguoiTaoId = gvId;
            await db.SaveChangesAsync(Ct);
        }

        await using (var scope = factory.CreateScope())
        {
            var hp = await scope.ServiceProvider.GetRequiredService<UpmDbContext>().HocPhan.AsNoTracking().SingleAsync(h => h.Id == hpId, Ct);
            Assert.Equal(adminId, hp.NguoiTaoId);
            Assert.InRange(hp.NgayTao, lucTao.AddSeconds(-5), lucTao.AddSeconds(5));
            Assert.Equal(gvId, hp.NguoiSuaId);
            Assert.NotNull(hp.NgaySua);
            Assert.Equal(3, hp.SoTC);
        }
    }

    [Fact]
    public async Task Job_he_thong_khong_co_nguoi_dung_thi_NguoiTaoId_NULL()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var hp = new HocPhan { MaHP = "UT-AUDITCOL-2", TenHP = "Job hệ thống", SoTC = 1 };
        db.HocPhan.Add(hp);
        await db.SaveChangesAsync(Ct);

        Assert.Null(hp.NguoiTaoId);
        Assert.NotEqual(default, hp.NgayTao);
    }

    internal static void GiaLapNguoiDung(AsyncServiceScope scope, long taiKhoanId, string ip = "10.0.0.1")
    {
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", taiKhoanId.ToString(System.Globalization.CultureInfo.InvariantCulture))], "Test")),
        };
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;
    }
}
