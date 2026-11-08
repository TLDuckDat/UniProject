using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Persistence.Seed;

namespace Upm.Api.IntegrationTests.Persistence;

[Collection(SharedApiFixture.Name)]
public sealed class DevDataSeederTests(UpmApiFactory factory)
{
    [Fact]
    public async Task Seed_tao_du_lieu_mau_va_chay_lai_khong_nhan_doi()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct);
        await SeedAsync(ct); // idempotent

        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        Assert.Equal(4, await db.Khoa.CountAsync(ct));
        Assert.Equal(2, await db.Khoa.CountAsync(k => k.KhoiNganh == KhoiNganh.KyThuat, ct));
        Assert.Equal(8, await db.BoMon.CountAsync(ct));
        Assert.Equal(6, await db.GiangVien.CountAsync(ct));
        Assert.Equal(4, await db.ChuongTrinhDaoTao.CountAsync(ct));
        Assert.Equal(20, await db.SinhVien.CountAsync(ct));
        Assert.Equal(20, await db.ThongTinTienDoSV.CountAsync(ct));
        Assert.Equal(16, await db.ThamSoBoMon.CountAsync(ct)); // 8 bộ môn × 2 RuleSet

        var hpTheoCtdt = await db.CTDTHocPhan.GroupBy(x => x.CTDTId).Select(g => g.Count()).ToListAsync(ct);
        Assert.All(hpTheoCtdt, n => Assert.InRange(n, 15, 16));
    }

    [Theory]
    [InlineData("65HN001", 0)]  // đủ điều kiện
    [InlineData("65HN002", 6)]  // A01
    [InlineData("65HN003", 14)] // A02
    [InlineData("65HN004", 10)] // A03 – biên đúng 10
    [InlineData("65HN006", 3)]  // A05 – GDQP/GDTC không tính
    [InlineData("64HCM017", 0)] // học lại đã đạt
    public async Task Kich_ban_no_tin_chi_dung_so_TC(string maSV, int tcNoMongDoi)
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct);

        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();

        var diem = await (from bd in db.BangDiem
                          join sv in db.SinhVien on bd.SinhVienId equals sv.Id
                          join hp in db.HocPhan on bd.HocPhanId equals hp.Id
                          where sv.MaSV == maSV
                          select new { hp.Id, hp.SoTC, hp.LoaiHP, bd.TrangThaiDiem }).ToListAsync(ct);

        // Chỉ đếm học phần THƯỜNG đã rớt mà chưa có lần học đạt (logic đầy đủ ở rule engine Phần 4).
        var tcNo = diem.Where(d => d.LoaiHP == LoaiHocPhan.Thuong)
            .GroupBy(d => d.Id)
            .Where(g => g.Any(d => d.TrangThaiDiem == TrangThaiDiem.Rot) && g.All(d => d.TrangThaiDiem != TrangThaiDiem.Dat))
            .Sum(g => g.First().SoTC);

        Assert.Equal(tcNoMongDoi, tcNo);
    }

    [Fact]
    public async Task Diem_dang_hoc_I_X_khong_co_diem_so()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct);

        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var chuaCoDiem = await db.BangDiem
            .Where(b => b.TrangThaiDiem == TrangThaiDiem.DangHoc || b.TrangThaiDiem == TrangThaiDiem.I || b.TrangThaiDiem == TrangThaiDiem.X)
            .ToListAsync(ct);

        Assert.NotEmpty(chuaCoDiem);
        Assert.All(chuaCoDiem, b => Assert.Null(b.Diem10));
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        await using var scope = factory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync(ct);
    }
}
