using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>
/// Điều kiện đưa sinh viên vào đợt ĐATN (bước "Thêm SV vào đợt" của UC-1, trước khi xét G1–G3).
/// A11: đang nghỉ học tạm thời / bảo lưu → KHÔNG đưa vào đợt, vào đợt sau khi quay lại.
/// Buộc thôi học / thôi học vẫn được đưa vào để luật G2 ghi nhận KHONG_DU_DK (A12) có snapshot giải trình.
/// </summary>
public static class DuaVaoDot
{
    public static KetQuaLuat KiemTra(TrangThaiHocTap trangThaiHocTap) => trangThaiHocTap switch
    {
        TrangThaiHocTap.NghiTamThoi => KetQuaLuat.KhongQua(MaLuat.DuaVaoDot,
            "A11: đang nghỉ học tạm thời / bảo lưu – không đưa vào đợt, vào đợt sau khi quay lại.", "A11"),
        TrangThaiHocTap.DaTotNghiep => KetQuaLuat.KhongQua(MaLuat.DuaVaoDot, "Sinh viên đã tốt nghiệp."),
        _ => KetQuaLuat.Qua(MaLuat.DuaVaoDot, "Được đưa vào đợt để xét điều kiện giao."),
    };
}
