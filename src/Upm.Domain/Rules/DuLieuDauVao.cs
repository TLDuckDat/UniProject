using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>Một học phần trong CTĐT của SV (từ <c>HocPhan</c> + <c>CTDTHocPhan</c>).</summary>
/// <param name="TinhDiem">false = tự chọn không tính điểm → không xét (mục 6 chỉ duyệt "hocPhanTinhDiem").</param>
/// <param name="LaHPThayTheDATN">Học phần chuyên môn thay thế ĐATN – không bắt buộc với SV làm ĐATN.</param>
public sealed record HocPhanCTDT(
    long HocPhanId,
    string MaHP,
    int SoTC,
    LoaiHocPhan Loai,
    bool TinhDiem = true,
    bool LaHPThayTheDATN = false);

/// <summary>Một dòng <c>BangDiem</c> (một lần học của một học phần).</summary>
public sealed record DiemHocPhan(long HocPhanId, TrangThaiDiem TrangThai, int LanHoc = 1);

/// <summary>Dữ liệu học tập dùng chung cho xét giao (G3) và xét bảo vệ (K4, K6).</summary>
public sealed record DuLieuHocTapSV
{
    public required IReadOnlyList<HocPhanCTDT> HocPhanCTDT { get; init; }

    public required IReadOnlyList<DiemHocPhan> BangDiem { get; init; }

    /// <summary>Kỳ cuối theo kế hoạch chuẩn (<c>ThongTinTienDoSV.KyCuoiKeHoach</c>).</summary>
    public required HocKy KyCuoiKeHoach { get; init; }
}

/// <summary>Mức kỷ luật (CTSV), xếp tăng dần theo mức nặng.</summary>
public enum MucKyLuat
{
    KhienTrach = 1,
    CanhCao = 2,
    DinhChiCoThoiHan = 3,
    BuocThoiHoc = 4,
}

/// <summary>Một quyết định kỷ luật / truy cứu (từ <c>KyLuatSV</c>).</summary>
public sealed record KyLuat(MucKyLuat Muc, DateOnly TuNgay, DateOnly? DenNgay, bool DinhChiHocTap, bool TruyCuuHinhSu)
{
    public bool HieuLucTai(DateOnly ngay) => TuNgay <= ngay && (DenNgay is null || ngay <= DenNgay);

    /// <summary>G1/K1: đang bị truy cứu TNHS hoặc đang đình chỉ học tập tại ngày xét.</summary>
    public bool ChanTai(DateOnly ngay) => (DinhChiHocTap || TruyCuuHinhSu) && HieuLucTai(ngay);
}
