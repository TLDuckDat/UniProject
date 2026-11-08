using Upm.Domain.Enums;

namespace Upm.Domain.Tests.Enums;

public class TrangThaiSVTrongDotTests
{
    [Fact]
    public void Gom_dung_17_trang_thai_theo_thiet_ke_muc_2()
    {
        string[] expected =
        [
            "CHO_XET", "KHONG_DU_DK", "KHONG_DU_DK_CHUYEN_THAY_THE", "DA_GIAO", "DANG_THUC_HIEN", "TAM_DUNG",
            "RUT", "DA_NOP_QUYEN", "YEU_CAU_CHINH_SUA", "DUOC_BAO_VE", "HOAN_BAO_VE", "PHAI_LAM_LAI",
            "DA_BAO_VE", "DAT", "KHONG_DAT", "CHO_BAN_CUOI", "HOAN_TAT",
        ];

        Assert.Equal(expected.Order(), Enum.GetNames<TrangThaiSVTrongDot>().Order());
    }

    [Fact]
    public void Ten_trang_thai_vua_cot_nvarchar_30_cua_LichSuTrangThai()
    {
        Assert.All(Enum.GetNames<TrangThaiSVTrongDot>(), name => Assert.True(name.Length <= 30, name));
    }
}
