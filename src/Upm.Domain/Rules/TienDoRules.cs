using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>
/// Luật tiến độ 3 mốc (đặc tả mục 5, C01–C07). Hàm thuần; Application dựng dữ liệu đầu vào.
/// Mã ngoại lệ C01…C07 gắn vào <see cref="KetQuaLuat.MaNgoaiLe"/>.
/// </summary>
public static class TienDoRules
{
    /// <summary>Nhắc trước hạn mốc (ngày).</summary>
    public const int SoNgayNhacTruocHan = 3;

    /// <summary>Yêu cầu tối thiểu mặc định của mốc M2 (60–70%, cấu hình được).</summary>
    public const decimal YeuCauM2MacDinh = 60m;

    /// <summary>Tuần thực hiện của từng mốc (đặc tả mục 5).</summary>
    public static (byte TuanTu, byte TuanDen) TuanMoc(MaMocTienDo moc) => moc switch
    {
        MaMocTienDo.M1 => (3, 4),
        MaMocTienDo.M2 => (8, 9),
        _ => (12, 13),
    };

    /// <summary>
    /// Hạn mặc định: cuối tuần 4 (M1), cuối tuần 9 (M2), ngày nộp quyển của đợt (M3).
    /// </summary>
    public static DateOnly HanMacDinh(MaMocTienDo moc, DateOnly ngayBatDau, DateOnly ngayNopQuyenHan) => moc switch
    {
        MaMocTienDo.M1 => ngayBatDau.AddDays(7 * 4 - 1),
        MaMocTienDo.M2 => ngayBatDau.AddDays(7 * 9 - 1),
        _ => ngayNopQuyenHan,
    };

    /// <summary>Tỉ lệ hoàn thành tối thiểu: M1 không đo %, M2 cấu hình (mặc định 60%), M3 = 100%.</summary>
    public static decimal? YeuCauToiThieu(MaMocTienDo moc, decimal? cauHinhM2) => moc switch
    {
        MaMocTienDo.M1 => null,
        MaMocTienDo.M2 => cauHinhM2 ?? YeuCauM2MacDinh,
        _ => 100m,
    };

    /// <summary>Phân loại báo cáo đã nộp: dưới yêu cầu → CanhBao (C02); đạt hoặc không yêu cầu % → Dat.</summary>
    public static KetQuaTienDo PhanLoai(decimal tyLeHoanThanh, decimal? yeuCau) =>
        yeuCau is { } y && tyLeHoanThanh < y ? KetQuaTienDo.CanhBao : KetQuaTienDo.Dat;

    /// <summary>Trạng thái mốc của một hồ sơ: đã có báo cáo → kết quả của báo cáo; quá hạn chưa nộp → KhongNop (C01).</summary>
    public static KetQuaTienDo? TrangThaiMoc(KetQuaTienDo? ketQuaBaoCao, DateOnly homNay, DateOnly han) =>
        ketQuaBaoCao ?? (QuaHan(homNay, han) ? KetQuaTienDo.KhongNop : null);

    /// <summary>Gần đến hạn (trong <see cref="SoNgayNhacTruocHan"/> ngày) và chưa quá hạn.</summary>
    public static bool NenNhacTruoc(DateOnly homNay, DateOnly han) =>
        han >= homNay && han.DayNumber - homNay.DayNumber <= SoNgayNhacTruocHan;

    public static bool QuaHan(DateOnly homNay, DateOnly han) => homNay > han;

    /// <summary>
    /// C04: bỏ 2 mốc trở lên → hệ thống chỉ GỢI Ý để Bộ môn cân nhắc RUT. Không tự chuyển trạng thái.
    /// </summary>
    public static bool NenGoiYRut(IEnumerable<KetQuaTienDo?> cacMoc) =>
        cacMoc.Count(x => x == KetQuaTienDo.KhongNop) >= 2;

    /// <summary>C07: báo cáo phải kèm cam kết không dùng AI viết hộ / bịa số liệu.</summary>
    public static KetQuaLuat KiemTraCamKetAI(bool camKet) => camKet
        ? KetQuaLuat.Qua(MaLuat.TienDoCamKetAI, "Đã xác nhận cam kết không dùng AI viết hộ hoặc bịa số liệu.", "C07")
        : KetQuaLuat.KhongQua(MaLuat.TienDoCamKetAI, "Cần xác nhận cam kết không dùng AI viết hộ hoặc bịa số liệu.", "C07");

    /// <summary>C05: đơn gia hạn chỉ có hiệu lực khi hạn mới sau hạn cũ.</summary>
    public static KetQuaLuat KiemTraHanGiaHan(DateOnly hanCu, DateOnly hanMoi) => hanMoi > hanCu
        ? KetQuaLuat.Qua(MaLuat.TienDoGiaHan, $"Gia hạn đến {hanMoi:dd/MM/yyyy}.", "C05")
        : KetQuaLuat.KhongQua(MaLuat.TienDoGiaHan, "Hạn mới phải sau hạn cũ.", "C05");
}
