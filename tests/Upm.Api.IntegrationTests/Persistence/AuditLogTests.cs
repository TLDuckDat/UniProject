using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Abstractions;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.Persistence;

/// <summary>AuditLogInterceptor ghi INSERT/UPDATE/DELETE với giá trị cũ/mới, người dùng, IP, SoQDBatBuoc.</summary>
[Collection(SharedApiFixture.Name)]
public sealed class AuditLogTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Update_ghi_gia_tri_cu_moi_chi_cac_cot_doi_kem_nguoi_dung_IP()
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var adminId = await db.TaiKhoan.Where(t => t.TenDangNhap == "admin").Select(t => t.Id).SingleAsync(Ct);
        GiaLapRequest(scope, taiKhoanId: adminId, ip: "10.0.0.7");

        var gv = await db.GiangVien.SingleAsync(g => g.MaGV == "GV002", Ct);
        var hocViCu = gv.HocVi;
        gv.HocVi = "PGS.TS";
        gv.QuotaToiDa += 1;
        await db.SaveChangesAsync(Ct);

        var log = await LogMoiNhatAsync(db, "GiangVien", gv.Id);
        Assert.Equal(HanhDongAudit.Update, log.HanhDong);
        Assert.Equal(adminId, log.NguoiDungId);
        Assert.Equal("10.0.0.7", log.IpAddress);
        Assert.Equal(gv.CoSoId, log.CoSoId);

        var cu = Doc(log.GiaTriCu);
        var moi = Doc(log.GiaTriMoi);
        Assert.Equal(["HocVi", "QuotaToiDa"], cu.Keys.Order());
        Assert.Equal(hocViCu, cu["HocVi"].GetString());
        Assert.Equal("PGS.TS", moi["HocVi"].GetString());
        Assert.Equal(cu["QuotaToiDa"].GetInt32() + 1, moi["QuotaToiDa"].GetInt32());
    }

    [Fact]
    public async Task Insert_co_Id_moi_va_Delete_ghi_gia_tri_cu()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        var hp = new HocPhan { MaHP = "UT-AUDIT-01", TenHP = "Học phần kiểm thử nhật ký", SoTC = 2 };
        db.HocPhan.Add(hp);
        await db.SaveChangesAsync(Ct);

        var them = await LogMoiNhatAsync(db, "HocPhan", hp.Id);
        Assert.Equal(HanhDongAudit.Insert, them.HanhDong);
        Assert.Null(them.GiaTriCu);
        Assert.Equal(hp.Id, Doc(them.GiaTriMoi)["Id"].GetInt64());
        Assert.Equal("Học phần kiểm thử nhật ký", Doc(them.GiaTriMoi)["TenHP"].GetString()); // tiếng Việt không bị escape lỗi
        Assert.Null(them.NguoiDungId); // không có request → hệ thống

        db.HocPhan.Remove(hp);
        await db.SaveChangesAsync(Ct);

        var xoa = await LogMoiNhatAsync(db, "HocPhan", hp.Id);
        Assert.Equal(HanhDongAudit.Delete, xoa.HanhDong);
        Assert.Equal("UT-AUDIT-01", Doc(xoa.GiaTriCu)["MaHP"].GetString());
        Assert.Null(xoa.GiaTriMoi);
    }

    [Fact]
    public async Task Ghi_SoQDBatBuoc_va_che_cot_mat_khau()
    {
        // Tài khoản riêng của test (không làm hỏng mật khẩu tài khoản dev dùng ở test khác).
        var id = await factory.TaoTaiKhoanAsync("audit.che.mk", "Upm@123456", Ct);
        await using var scope = factory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IAuditContext>().SoQDBatBuoc = "123/QĐ-ĐHGTVT";
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        var tk = await db.TaiKhoan.SingleAsync(t => t.Id == id, Ct);
        tk.MatKhauHash = "hash-moi";
        await db.SaveChangesAsync(Ct);

        var log = await LogMoiNhatAsync(db, "TaiKhoan", tk.Id);
        Assert.Equal("123/QĐ-ĐHGTVT", log.SoQDBatBuoc);
        Assert.Equal("***", Doc(log.GiaTriMoi)["MatKhauHash"].GetString());
        Assert.DoesNotContain("hash-moi", log.GiaTriMoi!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Luu_that_bai_thi_khong_co_du_lieu_lan_nhat_ky()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        db.HocPhan.Add(new HocPhan { MaHP = "UT-AUDIT-02", TenHP = "A", SoTC = 1 });
        await db.SaveChangesAsync(Ct);
        var soLogTruoc = await db.AuditLog.CountAsync(Ct);

        db.ChangeTracker.Clear();
        db.HocPhan.Add(new HocPhan { MaHP = "UT-AUDIT-03", TenHP = "B", SoTC = 1 });
        db.HocPhan.Add(new HocPhan { MaHP = "UT-AUDIT-02", TenHP = "trùng mã", SoTC = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));

        db.ChangeTracker.Clear();
        Assert.Equal(soLogTruoc, await db.AuditLog.CountAsync(Ct));
        Assert.False(await db.HocPhan.AnyAsync(h => h.MaHP == "UT-AUDIT-03", Ct)); // rollback cả dòng hợp lệ
    }

    [Fact]
    public async Task Refresh_token_khong_bi_ghi_nhat_ky()
    {
        var (_, token, _) = await factory.DangNhapAsync("ldk.ct.hn", Ct);
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        Assert.False(await db.AuditLog.AnyAsync(a => a.LoaiDoiTuong == "RefreshToken", Ct));
        Assert.True(await db.RefreshToken.AnyAsync(r => r.TaiKhoanId == token.NguoiDung.Id, Ct));
    }

    [Theory]
    [InlineData("MatKhauHash", true)]
    [InlineData("TokenHash", true)]
    [InlineData("RefreshToken", true)]
    [InlineData("ApiSecret", true)]
    [InlineData("MatKhau", true)]
    [InlineData("HashSHA256", false)] // dấu vân tay file quyển – công khai, cần giữ để đối chiếu
    [InlineData("HoTen", false)]
    [InlineData("Diem10", false)]
    public void Che_moi_cot_mat_khau_token_hash(string cot, bool biChe)
    {
        Assert.Equal(biChe, Upm.Infrastructure.Persistence.Interceptors.AuditLogInterceptor.LaCotNhayCam(cot));
    }

    [Fact]
    public async Task Cot_audit_khong_lap_lai_trong_gia_tri_cu_moi()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var hp = new HocPhan { MaHP = "UT-AUDIT-04", TenHP = "X", SoTC = 1 };
        db.HocPhan.Add(hp);
        await db.SaveChangesAsync(Ct);

        var log = await LogMoiNhatAsync(db, "HocPhan", hp.Id);
        var moi = Doc(log.GiaTriMoi);
        Assert.DoesNotContain("NgayTao", moi.Keys);
        Assert.DoesNotContain("NguoiTaoId", moi.Keys);
    }

    private static void GiaLapRequest(AsyncServiceScope scope, long taiKhoanId, string ip)
    {
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", taiKhoanId.ToString(System.Globalization.CultureInfo.InvariantCulture))], "Test")),
        };
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;
    }

    private static Task<AuditLog> LogMoiNhatAsync(UpmDbContext db, string bang, long id) =>
        db.AuditLog.AsNoTracking()
            .Where(a => a.LoaiDoiTuong == bang && a.DoiTuongId == id)
            .OrderByDescending(a => a.Id)
            .FirstAsync(Ct);

    private static Dictionary<string, JsonElement> Doc(string? json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!)!;
}
