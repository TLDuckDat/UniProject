using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

public sealed class DuaVaoDotTests
{
    [Fact]
    public void A11_Dang_nghi_hoc_tam_thoi_khong_dua_vao_dot()
    {
        var kq = DuaVaoDot.KiemTra(TrangThaiHocTap.NghiTamThoi);

        Assert.False(kq.Dat);
        Assert.Equal("A11", kq.MaNgoaiLe);
        Assert.Equal(MaLuat.DuaVaoDot, kq.MaLuat);
    }

    [Theory]
    [InlineData(TrangThaiHocTap.DangHoc)]
    [InlineData(TrangThaiHocTap.BuocThoiHoc)] // A12: vẫn vào đợt để G2 ghi KHONG_DU_DK có snapshot
    [InlineData(TrangThaiHocTap.DaThoiHoc)]
    public void A12_Dang_hoc_hoac_thoi_hoc_van_dua_vao_dot_de_xet(TrangThaiHocTap trangThai) =>
        Assert.True(DuaVaoDot.KiemTra(trangThai).Dat);

    [Fact]
    public void Da_tot_nghiep_khong_dua_vao_dot() => Assert.False(DuaVaoDot.KiemTra(TrangThaiHocTap.DaTotNghiep).Dat);
}
