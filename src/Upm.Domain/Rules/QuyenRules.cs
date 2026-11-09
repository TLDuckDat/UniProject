using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>
/// Luật nộp quyển, ý kiến GVHD, đọc duyệt GVĐD, sửa gấp, đạo văn (đặc tả mục 7, D05–D13).
/// Hàm thuần; Application dựng dữ liệu. Mã ngoại lệ gắn vào <see cref="KetQuaLuat.MaNgoaiLe"/>.
/// </summary>
public static class QuyenRules
{
    /// <summary>D05: GVHD không đồng ý cho bảo vệ thì BẮT BUỘC có lý do.</summary>
    public static KetQuaLuat KiemTraLyDoKhongDongY(bool dongY, string? lyDo) => dongY || !string.IsNullOrWhiteSpace(lyDo)
        ? KetQuaLuat.Qua(MaLuat.QuyenYKienGVHD, "Ý kiến hợp lệ.", "D05")
        : KetQuaLuat.KhongQua(MaLuat.QuyenYKienGVHD, "GVHD không đồng ý phải ghi lý do.", "D05");

    /// <summary>D10: "Đồng ý" và "điểm" là hai trường bắt buộc riêng – đồng ý thì phải có điểm.</summary>
    public static KetQuaLuat KiemTraDiemGVDD(KetQuaDocDuyetLoai ketQua, decimal? diem) =>
        ketQua != KetQuaDocDuyetLoai.DongY || diem is not null
            ? KetQuaLuat.Qua(MaLuat.QuyenDocDuyet, "Kết luận và điểm hợp lệ.", "D10")
            : KetQuaLuat.KhongQua(MaLuat.QuyenDocDuyet, "Đồng ý cho bảo vệ thì phải nhập điểm GVĐD.", "D10");

    /// <summary>
    /// D06 + D08: sửa gấp chỉ mở vòng 1 (VongDoc = 1), hạn sửa = 1..<paramref name="soNgayToiDa"/> ngày kể từ hôm nay.
    /// Vòng 2 không cho sửa gấp nữa: GVĐD phải chọn đồng ý / không.
    /// </summary>
    public static KetQuaLuat KiemTraSuaGap(byte vongDoc, DateOnly homNay, DateOnly hanSuaDen, int soNgayToiDa)
    {
        if (vongDoc > XetBaoVe.SoVongSuaGapToiDa)
            return KetQuaLuat.KhongQua(MaLuat.QuyenDocDuyet, "Đã hết vòng sửa gấp (tối đa 1 vòng); chọn đồng ý hoặc không đồng ý.", "D08");
        var so = hanSuaDen.DayNumber - homNay.DayNumber;
        return so >= 1 && so <= soNgayToiDa
            ? KetQuaLuat.Qua(MaLuat.QuyenDocDuyet, $"Hạn sửa {so} ngày.", "D06")
            : KetQuaLuat.KhongQua(MaLuat.QuyenDocDuyet, $"Hạn sửa gấp phải từ 1 đến {soNgayToiDa} ngày.", "D06");
    }

    /// <summary>D12: nộp quyển sau hạn bị chặn trừ khi có gia hạn (Phòng ĐT / Bộ môn mở lại riêng, có log).</summary>
    public static KetQuaLuat KiemTraHanNopQuyen(DateOnly homNay, DateOnly hanNop, DateOnly? hanGiaHan)
    {
        if (homNay <= hanNop) return KetQuaLuat.Qua(MaLuat.QuyenHanNop, "Trong hạn nộp quyển.", "D12");
        if (hanGiaHan is { } g && homNay <= g)
            return KetQuaLuat.Qua(MaLuat.QuyenHanNop, $"Đã được gia hạn đến {g:dd/MM/yyyy}.", "D12");
        return KetQuaLuat.KhongQua(MaLuat.QuyenHanNop, "Quá hạn nộp quyển; cần gia hạn được duyệt.", "D12");
    }

    /// <summary>D09: GVĐD không phản hồi trong số ngày quy định → cảnh báo Trưởng BM, cho thay GVĐD (SV không bị lỗi).</summary>
    public static bool QuaHanDocDuyet(DateTime ngayNopBan, DateTime homNay, int soNgayToiDa) =>
        homNay > ngayNopBan.AddDays(soNgayToiDa);

    /// <summary>D11: tỉ lệ đạo văn vượt ngưỡng → chặn bảo vệ, mở hồ sơ liêm chính. Bằng ngưỡng vẫn qua.</summary>
    public static KetQuaLuat KiemTraDaoVan(decimal tyLeTrungPct, decimal nguongPct) => tyLeTrungPct > nguongPct
        ? KetQuaLuat.KhongQua(MaLuat.QuyenDaoVan, $"Tỉ lệ trùng {tyLeTrungPct}% vượt ngưỡng {nguongPct}%: chặn bảo vệ, mở hồ sơ liêm chính.", "D11")
        : KetQuaLuat.Qua(MaLuat.QuyenDaoVan, $"Tỉ lệ trùng {tyLeTrungPct}% không vượt ngưỡng {nguongPct}%.", "D11");

    /// <summary>D13: bản GVĐD đọc phải là bản sẽ bảo vệ – so SHA-256.</summary>
    public static KetQuaLuat KiemTraBanBaoVe(string hashBanDocDuyet, string hashBanBaoVe) =>
        string.Equals(hashBanDocDuyet, hashBanBaoVe, StringComparison.OrdinalIgnoreCase)
            ? KetQuaLuat.Qua(MaLuat.QuyenBanBaoVe, "Bản bảo vệ trùng bản GVĐD đã đọc.", "D13")
            : KetQuaLuat.KhongQua(MaLuat.QuyenBanBaoVe, "Bản bảo vệ khác bản GVĐD đã đọc (hash không khớp).", "D13");
}
