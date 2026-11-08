namespace Upm.Domain.Rules;

/// <summary>Xếp loại tốt nghiệp, tăng dần.</summary>
public enum XepLoaiTotNghiep
{
    TrungBinh,
    Kha,
    Gioi,
    XuatSac,
}

/// <summary>Dữ liệu xét hạ hạng tốt nghiệp.</summary>
/// <param name="TCHocLai">Tổng TC phải học lại (kể cả làm lại ĐATN – A21).</param>
/// <param name="TongTCCTDT">Tổng TC của CTĐT (<c>ChuongTrinhDaoTao.TongTCYeuCau</c>).</param>
public sealed record HoSoXetHaHang(
    XepLoaiTotNghiep XepLoai,
    int TCHocLai,
    int TongTCCTDT,
    IReadOnlyList<KyLuat> KyLuat,
    bool CoViPhamLiemChinh = false);

public sealed record KetQuaHaHang(
    XepLoaiTotNghiep XepLoaiBanDau,
    XepLoaiTotNghiep XepLoaiSauXet,
    decimal TyLeHocLaiPct,
    IReadOnlyList<string> CanCu,
    string RuleVersion) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.HaHang;

    public bool BiHaHang => XepLoaiSauXet != XepLoaiBanDau;

    /// <summary>Đạt = không bị hạ hạng.</summary>
    public bool Dat => !BiHaHang;

    public string LyDo => BiHaHang
        ? $"Hạ từ {XepLoaiBanDau} xuống {XepLoaiSauXet}: {string.Join("; ", CanCu)}."
        : $"Giữ xếp loại {XepLoaiBanDau}.";

    /// <summary>Đặc tả mục 10 không có mã riêng cho hạ hạng (A21 chỉ nói làm lại ĐATN được tính là học lại).</summary>
    public string? MaNgoaiLe => null;
}

/// <summary>Luật liên quan xét tốt nghiệp: GPA tối thiểu (T10) và hạ hạng Giỏi/Xuất sắc theo RuleSet.</summary>
public static class XetTotNghiep
{
    /// <summary>T10: GPA (làm tròn 2 chữ số) ≥ GpaTotNghiepMin (2,00).</summary>
    public static KetQuaLuat KiemTraGpa(decimal gpaTho, ThamSoQuyChe ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        var gpa = LamTron.Gpa(gpaTho);
        return gpa >= ruleSet.GpaTotNghiepMin
            ? KetQuaLuat.Qua(MaLuat.GpaTotNghiep, $"GPA {gpa:0.00} ≥ {ruleSet.GpaTotNghiepMin:0.00}.")
            : KetQuaLuat.KhongQua(MaLuat.GpaTotNghiep, $"GPA {gpa:0.00} < {ruleSet.GpaTotNghiepMin:0.00}: chưa đủ điều kiện tốt nghiệp.");
    }

    /// <summary>
    /// Hạ một mức (Xuất sắc → Giỏi, Giỏi → Khá) khi:
    /// TC học lại / tổng TC VƯỢT <c>NguongHaHangHocLaiPct</c> (UTC-2021: 5%, UTC-2026: 10% ⚠O6);
    /// hoặc <c>HaHangKhiKyLuat</c> và có kỷ luật từ cảnh cáo trở lên;
    /// hoặc <c>HaHangKhiViPhamLiemChinh</c> và có vi phạm liêm chính học thuật.
    /// </summary>
    public static KetQuaHaHang HaHang(HoSoXetHaHang hoSo, ThamSoQuyChe ruleSet)
    {
        ArgumentNullException.ThrowIfNull(hoSo);
        ArgumentNullException.ThrowIfNull(ruleSet);
        if (hoSo.TongTCCTDT <= 0 || hoSo.TCHocLai < 0)
        {
            throw new Common.DomainException("UPM_HA_HANG_DU_LIEU_KHONG_HOP_LE", "Tổng TC của CTĐT phải > 0 và TC học lại ≥ 0.");
        }

        var tyLe = hoSo.TCHocLai * 100m / hoSo.TongTCCTDT;
        var canCu = new List<string>();
        if (tyLe > ruleSet.NguongHaHangHocLaiPct)
        {
            canCu.Add($"học lại {hoSo.TCHocLai}/{hoSo.TongTCCTDT} TC ({LamTron.Gpa(tyLe):0.00}%) > {ruleSet.NguongHaHangHocLaiPct:0.0}%");
        }

        if (ruleSet.HaHangKhiKyLuat && hoSo.KyLuat.Any(k => k.Muc >= MucKyLuat.CanhCao))
        {
            canCu.Add("bị kỷ luật từ mức cảnh cáo trở lên");
        }

        if (ruleSet.HaHangKhiViPhamLiemChinh && hoSo.CoViPhamLiemChinh)
        {
            canCu.Add("vi phạm liêm chính học thuật");
        }

        var sau = canCu.Count > 0 && hoSo.XepLoai >= XepLoaiTotNghiep.Gioi ? hoSo.XepLoai - 1 : hoSo.XepLoai;
        return new KetQuaHaHang(hoSo.XepLoai, sau, tyLe, canCu, ruleSet.MaRuleSet);
    }
}
