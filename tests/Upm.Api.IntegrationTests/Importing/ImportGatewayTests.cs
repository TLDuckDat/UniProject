using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.DongBo;

namespace Upm.Api.IntegrationTests.Importing;

[Collection(SharedApiFixture.Name)]
public sealed class ImportGatewayTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("8,5", 8.5)]
    [InlineData("8.5", 8.5)]
    public void Decimal_import_ho_tro_dau_phay_va_dau_cham(string text, decimal expected)
    {
        Assert.True(ImportValueParser.TryDecimal(text, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Xem_truoc_bao_loi_theo_dong_va_chua_ghi_du_lieu()
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IQldtGateway>();
        var content = "MaSV;MaHP;HocKy;LanHoc;Diem10;DiemChu;Diem4;TrangThaiDiem\r\nNO-SUCH-STUDENT;NO-SUCH-COURSE;2026.1;1;8,5;B;3.5;Dat\r\n";
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var preview = await gateway.XemTruocAsync(LoaiImport.BangDiem, "diem.csv", input, Ct);

        Assert.Equal(1, preview.TotalRows);
        Assert.Equal(0, preview.ValidRows);
        Assert.Contains(preview.Errors, x => x.Row == 2 && x.Field == "MaSV");
        Assert.Contains(preview.Errors, x => x.Row == 2 && x.Field == "MaHP");
    }
}
