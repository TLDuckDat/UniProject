namespace Upm.Domain.Enums;

/// <summary>
/// Trạng thái hồ sơ ĐATN của một SV trong một đợt (docs/thiet-ke.md mục 2, T01–T22).
/// Chỉ được đổi qua <c>DATNWorkflow.Transition()</c> (CLAUDE.md §5.3).
/// Tên thành viên = giá trị lưu DB (nvarchar(30)) – giữ nguyên dạng UPPER_SNAKE như đặc tả.
/// </summary>
public enum TrangThaiSVTrongDot
{
    CHO_XET,
    KHONG_DU_DK,
    KHONG_DU_DK_CHUYEN_THAY_THE,
    DA_GIAO,
    DANG_THUC_HIEN,
    TAM_DUNG,
    RUT,
    DA_NOP_QUYEN,
    YEU_CAU_CHINH_SUA,
    DUOC_BAO_VE,
    HOAN_BAO_VE,
    PHAI_LAM_LAI,
    DA_BAO_VE,
    DAT,
    KHONG_DAT,
    CHO_BAN_CUOI,
    HOAN_TAT,
}
