using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Workflow;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.Workflow;

[Collection(SharedApiFixture.Name)]
public sealed class WorkflowConcurrencyTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RowVersion_chan_thao_tac_thu_hai_dung_du_lieu_cu()
    {
        await factory.SeedAsync(Ct);
        long hoSoId;
        byte[] rowVersion;
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var account = await db.TaiKhoan.SingleAsync(x => x.TenDangNhap == "sv.65hn001", Ct);
            var sinhVienId = account.SinhVienId!.Value;
            var student = await db.SinhVien.SingleAsync(x => x.Id == sinhVienId, Ct);
            var dot = new DotDATN
            {
                CoSoId = student.CoSoId,
                MaDot = $"WF-{Guid.NewGuid():N}"[..16],
                TenDot = "Đợt kiểm thử workflow",
                NamHoc = "2026-2027",
                HocKy = 1,
                NgayBatDau = factory.Clock.Today.AddDays(5),
                SoTuan = 12,
                NgayNopQuyenHan = factory.Clock.Today.AddDays(60),
                TrangThai = TrangThaiDot.Mo,
            };
            db.DotDATN.Add(dot);
            await db.SaveChangesAsync(Ct);

            var record = new SVTrongDot
            {
                DotId = dot.Id,
                SinhVienId = student.Id,
                CTDTId = student.CTDTId,
                CoSoId = student.CoSoId,
                TrangThai = TrangThaiSVTrongDot.DAT,
            };
            db.SVTrongDot.Add(record);
            await db.SaveChangesAsync(Ct);
            hoSoId = record.Id;
            rowVersion = record.RowVersion;
        }

        var login = await factory.DangNhapAsync("sv.65hn001", Ct);
        using var client = login.Client;
        var definitions = await client.GetFromJsonAsync<JsonElement>("/api/v1/workflow/dinh-nghia", Ct);
        Assert.Equal(24, definitions.GetArrayLength()); // T01–T22 + T01R, T02R (A19)

        var available = await client.GetFromJsonAsync<JsonElement>($"/api/v1/svtrongdot/{hoSoId}/chuyen-hop-le", Ct);
        var actions = available.GetProperty("chuyenHopLe");
        Assert.Contains(actions.EnumerateArray(), x => x.GetProperty("maChuyen").GetString() == "T21");
        var staleVersion = Convert.ToBase64String(rowVersion);
        Assert.Equal(staleVersion, available.GetProperty("hoSo").GetProperty("rowVersion").GetString());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>($"/api/v1/svtrongdot/{hoSoId}/lich-su-trang-thai", Ct)).EnumerateArray());

        var request = new WorkflowTransitionRequest(staleVersion, MaChuyen: "T21");
        var first = await client.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSoId}/chuyen-trang-thai", request, Ct);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstResult = (await first.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("rowVersion").GetString();
        Assert.NotEqual(staleVersion, firstResult);

        var second = await client.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSoId}/chuyen-trang-thai", request, Ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("UPM_WORKFLOW_CONCURRENCY_CONFLICT", problem.GetProperty("code").GetString());

        await using var verifyScope = factory.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UpmDbContext>();
        Assert.Equal(TrangThaiSVTrongDot.CHO_BAN_CUOI,
            await verifyDb.SVTrongDot.Where(x => x.Id == hoSoId).Select(x => x.TrangThai).SingleAsync(Ct));
        Assert.Single(await verifyDb.LichSuTrangThai.Where(x => x.SVTrongDotId == hoSoId).ToArrayAsync(Ct));
        Assert.Contains(await verifyDb.AuditLog.Where(x => x.LoaiDoiTuong == "SVTrongDot" && x.DoiTuongId == hoSoId).ToArrayAsync(Ct),
            x => x.HanhDong == HanhDongAudit.Update);
    }
}
