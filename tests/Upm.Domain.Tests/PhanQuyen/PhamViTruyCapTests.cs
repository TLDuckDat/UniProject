using Upm.Domain.PhanQuyen;

namespace Upm.Domain.Tests.PhanQuyen;

/// <summary>Lọc phạm vi cơ sở → khoa → bộ môn (CLAUDE.md §5.7), chạy trên LINQ-to-Objects.</summary>
public class PhamViTruyCapTests
{
    private const long HN = 1, HCM = 2;
    private const long KhoaCT_HN = 10, KhoaKT_HN = 11, KhoaCT_HCM = 20;
    private const long BmCau_HN = 100, BmDuong_HN = 101;

    private sealed record Ban(string Ten, long CoSo, long? Khoa, long? BoMon);

    private static readonly Ban[] DuLieu =
    [
        new("Cầu HN", HN, KhoaCT_HN, BmCau_HN),
        new("Đường HN", HN, KhoaCT_HN, BmDuong_HN),
        new("Kinh tế HN", HN, KhoaKT_HN, null),
        new("Công trình HCM", HCM, KhoaCT_HCM, null),
    ];

    private static string[] Loc(PhamViTruyCap phamVi) =>
        DuLieu.AsQueryable()
            .Where(phamVi.TaoBieuThuc<Ban>(x => x.CoSo, x => x.Khoa, x => x.BoMon))
            .Select(x => x.Ten)
            .ToArray();

    [Fact]
    public void Toan_he_thong_khi_ca_3_cot_NULL_thay_tat_ca()
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.QuanTri, null, null, null)]);

        Assert.Equal(DuLieu.Select(x => x.Ten), Loc(phamVi));
    }

    [Fact]
    public void Pham_vi_co_so_thay_moi_khoa_bo_mon_cua_co_so_do()
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.PhongDaoTao, HN, null, null)]);

        Assert.Equal(["Cầu HN", "Đường HN", "Kinh tế HN"], Loc(phamVi));
    }

    [Fact]
    public void GV_khoa_A_khong_thay_du_lieu_khoa_B()
    {
        // CoSoId đã được suy ra từ khoa khi nạp quyền – không được hiểu là "cả cơ sở".
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.GiangVien, HN, KhoaCT_HN, null)]);

        Assert.Equal(["Cầu HN", "Đường HN"], Loc(phamVi));
    }

    [Fact]
    public void Pham_vi_bo_mon_chi_thay_bo_mon_do()
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.TruongBoMon, HN, KhoaCT_HN, BmCau_HN)]);

        Assert.Equal(["Cầu HN"], Loc(phamVi));
    }

    [Fact]
    public void Nhieu_vai_tro_thi_hop_pham_vi()
    {
        var phamVi = PhamViTruyCap.Tu(
        [
            new PhamViVaiTro(VaiTroMa.TruongBoMon, HN, KhoaCT_HN, BmCau_HN),
            new PhamViVaiTro(VaiTroMa.GiangVien, HCM, KhoaCT_HCM, null),
        ]);

        Assert.Equal(["Cầu HN", "Công trình HCM"], Loc(phamVi));
    }

    [Fact]
    public void Chi_vai_tro_chi_dinh_moi_duoc_tinh()
    {
        var vaiTro = new[]
        {
            new PhamViVaiTro(VaiTroMa.TruongBoMon, HN, KhoaCT_HN, BmCau_HN),
            new PhamViVaiTro(VaiTroMa.PhongDaoTao, HCM, null, null),
        };

        Assert.Equal(["Cầu HN"], Loc(PhamViTruyCap.Tu(vaiTro, chiVaiTro: [VaiTroMa.TruongBoMon])));
    }

    [Fact]
    public void Co_so_dang_lam_viec_thu_hep_pham_vi_toan_he_thong()
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.QuanTri, null, null, null)], coSoDangLamViecId: HCM);

        Assert.Equal(["Công trình HCM"], Loc(phamVi));
    }

    [Fact]
    public void Co_so_dang_lam_viec_khong_mo_rong_pham_vi()
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.GiangVien, HN, KhoaCT_HN, null)], coSoDangLamViecId: HCM);

        Assert.Empty(Loc(phamVi));
    }

    [Fact]
    public void Khong_co_vai_tro_thi_khong_thay_gi()
    {
        Assert.True(PhamViTruyCap.Rong.TrongRong);
        Assert.Empty(Loc(PhamViTruyCap.Tu([])));
    }

    [Theory]
    [InlineData(HN, KhoaCT_HN, BmCau_HN, true)]
    [InlineData(HN, KhoaKT_HN, null, false)]
    [InlineData(HCM, KhoaCT_HCM, null, false)]
    public void ChoPhep_kiem_tung_ban_ghi(long coSo, long? khoa, long? boMon, bool mongDoi)
    {
        var phamVi = PhamViTruyCap.Tu([new PhamViVaiTro(VaiTroMa.GiangVien, HN, KhoaCT_HN, null)]);

        Assert.Equal(mongDoi, phamVi.ChoPhep(coSo, khoa, boMon));
    }

    [Fact]
    public void BangQuyen_hop_quyen_cac_vai_tro()
    {
        var quyen = BangQuyen.ChoVaiTro([VaiTroMa.GiangVien, VaiTroMa.TruongBoMon]);

        Assert.Contains(Quyen.PhanCongQuanLy, quyen);
        Assert.Contains(Quyen.DeTaiDeXuat, quyen);
        Assert.DoesNotContain(Quyen.DiemChot, quyen);
        Assert.Empty(BangQuyen.ChoVaiTro(["VaiTroLa"]));
    }
}
