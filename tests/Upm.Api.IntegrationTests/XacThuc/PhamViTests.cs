using System.Net;
using System.Net.Http.Json;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Common;
using Upm.Application.TraCuuSinhVien;
using Upm.Application.XacThuc;

namespace Upm.Api.IntegrationTests.XacThuc;

/// <summary>
/// Lọc dữ liệu theo phạm vi cơ sở → khoa (CLAUDE.md §5.7) qua GET /api/v1/sinh-vien.
/// Dữ liệu mẫu: HN-Công trình 12 SV, HN-Kinh tế 2, HCM-Công trình 2, HCM-Kinh tế 4 (tổng 20).
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class PhamViTests(UpmApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GV_khoa_Cong_trinh_HN_khong_thay_SV_khoa_khac()
    {
        var ds = await DanhSachAsync("gv001");

        Assert.Equal(12, ds.Total);
        Assert.All(ds.Items, sv =>
        {
            Assert.Equal("HN", sv.MaCoSo);
            Assert.Equal("Khoa Công trình (Hà Nội)", sv.TenKhoa);
        });
    }

    [Fact]
    public async Task GV_khoa_Kinh_te_HN_chi_thay_SV_khoa_minh()
    {
        var ds = await DanhSachAsync("gv003");

        Assert.Equal(2, ds.Total);
        Assert.All(ds.Items, sv => Assert.Equal("Khoa Kinh tế vận tải (Hà Nội)", sv.TenKhoa));
    }

    [Fact]
    public async Task Phong_DT_co_so_HN_thay_moi_khoa_HN_khong_thay_HCM()
    {
        var ds = await DanhSachAsync("pdt.hn");

        Assert.Equal(14, ds.Total);
        Assert.All(ds.Items, sv => Assert.Equal("HN", sv.MaCoSo));
    }

    [Fact]
    public async Task Quan_tri_toan_he_thong_thay_tat_ca()
    {
        Assert.Equal(20, (await DanhSachAsync("admin")).Total);
    }

    [Fact]
    public async Task Tai_khoan_2_co_so_chon_co_so_dang_lam_viec_qua_header()
    {
        var hcmId = await CoSoIdAsync("pdt.2coso", "HCM");

        Assert.Equal(20, (await DanhSachAsync("pdt.2coso")).Total);
        var ds = await DanhSachAsync("pdt.2coso", coSoId: hcmId);
        Assert.Equal(6, ds.Total);
        Assert.All(ds.Items, sv => Assert.Equal("HCM", sv.MaCoSo));
    }

    [Fact]
    public async Task Header_co_so_ngoai_quyen_bi_bo_qua_khong_mo_rong_pham_vi()
    {
        var hcmId = await CoSoIdAsync("admin", "HCM");

        var ds = await DanhSachAsync("pdt.hn", coSoId: hcmId);

        Assert.Equal(14, ds.Total);
        Assert.All(ds.Items, sv => Assert.Equal("HN", sv.MaCoSo));
    }

    [Theory]
    [InlineData("sv.65hn001")]
    [InlineData("thuvien.hn")]
    public async Task Vai_tro_khong_co_quyen_SinhVien_Xem_bi_403(string tenDangNhap)
    {
        var (client, _, _) = await factory.DangNhapAsync(tenDangNhap, Ct);

        var res = await client.GetAsync("/api/v1/sinh-vien", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    private async Task<PagedResult<SinhVienListItem>> DanhSachAsync(string tenDangNhap, long? coSoId = null)
    {
        var (client, _, _) = await factory.DangNhapAsync(tenDangNhap, Ct);
        if (coSoId is not null)
        {
            client.DefaultRequestHeaders.Add("X-CoSo-Id", coSoId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return (await client.GetFromJsonAsync<PagedResult<SinhVienListItem>>("/api/v1/sinh-vien?pageSize=100", Ct))!;
    }

    private async Task<long> CoSoIdAsync(string tenDangNhap, string maCoSo)
    {
        var (_, token, _) = await factory.DangNhapAsync(tenDangNhap, Ct);
        return token.NguoiDung.CoSoDuocPhep.Single(c => c.MaCoSo == maCoSo).Id;
    }
}
