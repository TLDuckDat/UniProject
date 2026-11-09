using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.RuleSets;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.RuleSets;

[Collection(SharedApiFixture.Name)]
public sealed class RuleSetLockTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RuleSet_da_co_snapshot_xet_khong_the_sua()
    {
        await factory.SeedAsync(Ct);
        long ruleSetId;
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var source = await db.RuleSet.AsNoTracking().FirstAsync(Ct);
            var version = new RuleSet
            {
                MaRuleSet = $"LOCK-{Guid.NewGuid():N}"[..20], TenRuleSet = "RuleSet lock test", MaxTCChuaTichLuy = source.MaxTCChuaTichLuy,
                GpaBaoVeMin = source.GpaBaoVeMin, GpaTotNghiepMin = source.GpaTotNghiepMin, DiemDatDATN = source.DiemDatDATN,
                TrongSoQT = source.TrongSoQT, HeSoThoiGianToiDa = source.HeSoThoiGianToiDa, MinTVHoiDong = source.MinTVHoiDong,
                MaxTVHoiDong = source.MaxTVHoiDong, NguongHaHangHocLaiPct = source.NguongHaHangHocLaiPct,
                HaHangKhiKyLuat = source.HaHangKhiKyLuat, HieuLucTu = source.HieuLucTu,
            };
            var student = await db.SinhVien.FirstAsync(Ct);
            var dot = new DotDATN
            {
                CoSoId = student.CoSoId, MaDot = $"RL-{Guid.NewGuid():N}"[..15], TenDot = "Đợt test RuleSet lock", NamHoc = "2026-2027",
                HocKy = 1, NgayBatDau = factory.Clock.Today, SoTuan = 12, NgayNopQuyenHan = factory.Clock.Today.AddDays(90), TrangThai = TrangThaiDot.Mo,
            };
            db.RuleSet.Add(version); db.DotDATN.Add(dot);
            await db.SaveChangesAsync(Ct);
            var profile = new SVTrongDot { DotId = dot.Id, SinhVienId = student.Id, CTDTId = student.CTDTId, CoSoId = student.CoSoId };
            db.SVTrongDot.Add(profile);
            await db.SaveChangesAsync(Ct);
            db.XetDieuKien.Add(new XetDieuKien
            {
                SVTrongDotId = profile.Id, LoaiXet = LoaiXet.Giao, RuleSetId = version.Id, RuleVersion = version.MaRuleSet,
                ThoiDiemXet = factory.Clock.Now.UtcDateTime, KetQua = "DU_DK", TCChuaTichLuy = 0, GpaTichLuy = 3.5m,
            });
            await db.SaveChangesAsync(Ct);
            ruleSetId = version.Id;
        }

        await using var serviceScope = factory.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IRuleSetService>();
        var rule = await service.LayAsync(ruleSetId, Ct);
        var request = new RuleSetRequest(rule.MaRuleSet, rule.TenRuleSet, rule.CanCuPhapLy, rule.MaxTCChuaTichLuy, rule.GpaBaoVeMin,
            rule.GpaTotNghiepMin, rule.DiemDatDATN, rule.TrongSoQT, rule.HeSoThoiGianToiDa, rule.MinTVHoiDong, rule.MaxTVHoiDong,
            rule.NguongHaHangHocLaiPct, rule.HaHangKhiKyLuat, rule.HieuLucTu, rule.HieuLucDen);

        var error = await Assert.ThrowsAsync<DomainException>(() => service.SuaAsync(ruleSetId, request, Ct));
        Assert.Equal("UPM_RULESET_DA_SU_DUNG", error.Code);
    }
}
