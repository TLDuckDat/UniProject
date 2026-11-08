using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>Kết luận xét giao ĐATN (mục 6). Tên = giá trị lưu <c>XetDieuKien.KetQua</c>.</summary>
public enum KetLuanXetGiao
{
    DU_DK,
    KHONG_DU_DK,
    KHONG_DU_DK_CHUYEN_THAY_THE,
}

/// <summary>Cờ kèm theo khi đủ điều kiện giao (lưu <c>SVTrongDot.CoNoTinChi</c>, <c>ChuaQuaTTTN</c>).</summary>
public enum CoXetGiao
{
    /// <summary>A01: được làm, phải trả nợ trước khi bảo vệ.</summary>
    NO_TIN_CHI,

    /// <summary>A06: bình thường với SV đúng tiến độ; chỉ chặn ở bước xét bảo vệ (K6).</summary>
    CHUA_QUA_TTTN,
}

/// <summary>Hồ sơ SV đầu vào xét giao.</summary>
public sealed record HoSoXetGiao
{
    public required TrangThaiHocTap TrangThaiHocTap { get; init; }

    public required IReadOnlyList<KyLuat> KyLuat { get; init; }

    /// <summary>Hạn chót học tập (<c>ThongTinTienDoSV.HanToiDa</c> hoặc <see cref="XetGiao.TinhHanToiDa"/>).</summary>
    public required DateOnly HanToiDa { get; init; }

    public required DuLieuHocTapSV HocTap { get; init; }

    /// <summary><c>ChuongTrinhDaoTao.CoHocPhanThayThe</c> – khối Kinh tế/Cử nhân (A14).</summary>
    public required bool CTDTCoHocPhanThayThe { get; init; }
}

/// <summary>Kết quả xét giao: kết luận, cờ, kết quả từng luật G1–G3 và chi tiết TC (snapshot).</summary>
public sealed record KetQuaXetGiao(
    KetLuanXetGiao KetLuan,
    IReadOnlyList<CoXetGiao> Co,
    IReadOnlyList<KetQuaLuat> CacLuat,
    KetQuaTCChuaTichLuy TinChi,
    HocKy KyXet,
    DateOnly NgayXet) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.XetGiao;

    public bool Dat => KetLuan == KetLuanXetGiao.DU_DK;

    public string RuleVersion => TinChi.RuleVersion;

    public string LyDo => KetLuan switch
    {
        KetLuanXetGiao.DU_DK => Co.Count == 0
            ? "Đủ điều kiện giao ĐATN."
            : $"Đủ điều kiện giao ĐATN, kèm cờ {string.Join(", ", Co)}.",
        KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE =>
            $"{TinChi.LyDo} CTĐT có học phần thay thế: chuyển đăng ký học phần chuyên môn thay thế ĐATN.",
        _ => string.Join(" ", CacLuat.Where(l => !l.Dat).Select(l => l.LyDo)),
    };

    /// <summary>
    /// Không đủ ĐK: mã của luật đầu tiên trượt (A10 G1; A11/A12 G2) hoặc A13/A14 khi trượt vì tín chỉ.
    /// Đủ ĐK: A03/A01 theo số TC nợ, hoặc A06 khi chỉ có cờ CHUA_QUA_TTTN.
    /// </summary>
    public string? MaNgoaiLe => KetLuan switch
    {
        KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE => "A14",
        KetLuanXetGiao.KHONG_DU_DK => CacLuat.First(l => !l.Dat) is { MaLuat: Rules.MaLuat.G3 } ? "A13" : CacLuat.First(l => !l.Dat).MaNgoaiLe,
        _ => TinChi.MaNgoaiLe ?? (Co.Contains(CoXetGiao.CHUA_QUA_TTTN) ? "A06" : null),
    };
}

/// <summary>Luật xét giao ĐATN G1–G3 (đặc tả mục 6). Hàm thuần: mọi dữ liệu và tham số truyền vào.</summary>
public static class XetGiao
{
    /// <summary>
    /// Luôn đánh giá đủ G1–G3 để snapshot có đầy đủ giải thích; kết luận theo thứ tự ưu tiên của pseudo-code
    /// (G1 → G2 → G3).
    /// </summary>
    public static KetQuaXetGiao Xet(HoSoXetGiao hoSo, ThamSoQuyChe ruleSet, HocKy kyXet, DateOnly ngayXet)
    {
        ArgumentNullException.ThrowIfNull(hoSo);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var g1 = XetG1(hoSo.KyLuat, ngayXet);
        var g2 = XetG2(hoSo, ngayXet);
        var tinChi = TinhTCChuaTichLuy.Tinh(hoSo.HocTap, ruleSet, kyXet);
        var g3 = new KetQuaLuat(MaLuat.G3, tinChi.Dat, tinChi.LyDo, tinChi.MaNgoaiLe);
        var cacLuat = new[] { g1, g2, g3 };

        KetLuanXetGiao ketLuan;
        var co = new List<CoXetGiao>();
        if (!g1.Dat || !g2.Dat)
        {
            ketLuan = KetLuanXetGiao.KHONG_DU_DK;
        }
        else if (!g3.Dat)
        {
            ketLuan = hoSo.CTDTCoHocPhanThayThe ? KetLuanXetGiao.KHONG_DU_DK_CHUYEN_THAY_THE : KetLuanXetGiao.KHONG_DU_DK;
        }
        else
        {
            ketLuan = KetLuanXetGiao.DU_DK;
            if (tinChi.TongTC > 0)
            {
                co.Add(CoXetGiao.NO_TIN_CHI);
            }

            if (!tinChi.DaDatTTTN)
            {
                co.Add(CoXetGiao.CHUA_QUA_TTTN);
            }
        }

        return new KetQuaXetGiao(ketLuan, co, cacLuat, tinChi, kyXet, ngayXet);
    }

    /// <summary>A12: hạn chót = ngày nhập học + hệ số (2,0) × thời gian đào tạo chuẩn (năm), tính theo tháng.</summary>
    public static DateOnly TinhHanToiDa(DateOnly ngayNhapHoc, decimal soNamChuan, decimal heSoThoiGianToiDa) =>
        ngayNhapHoc.AddMonths((int)Math.Round(soNamChuan * heSoThoiGianToiDa * 12m, MidpointRounding.AwayFromZero));

    /// <summary>G1 / K1: không truy cứu TNHS, không đang đình chỉ học tập tại ngày xét.</summary>
    internal static KetQuaLuat XetKyLuat(string maLuat, IReadOnlyList<KyLuat> kyLuat, DateOnly ngay, string maNgoaiLe)
    {
        ArgumentNullException.ThrowIfNull(kyLuat);
        var chan = kyLuat.Where(k => k.ChanTai(ngay)).ToList();
        if (chan.Count == 0)
        {
            return KetQuaLuat.Qua(maLuat, "Không bị truy cứu TNHS, không đang bị đình chỉ học tập.");
        }

        var lyDo = chan.Any(k => k.TruyCuuHinhSu)
            ? "Đang bị truy cứu trách nhiệm hình sự."
            : "Đang bị đình chỉ học tập.";
        return KetQuaLuat.KhongQua(maLuat, lyDo, maNgoaiLe);
    }

    private static KetQuaLuat XetG1(IReadOnlyList<KyLuat> kyLuat, DateOnly ngay) =>
        XetKyLuat(MaLuat.G1, kyLuat, ngay, "A10");

    private static KetQuaLuat XetG2(HoSoXetGiao hoSo, DateOnly ngayXet)
    {
        switch (hoSo.TrangThaiHocTap)
        {
            case TrangThaiHocTap.NghiTamThoi:
                return KetQuaLuat.KhongQua(MaLuat.G2, "Đang nghỉ học tạm thời / bảo lưu.", "A11");
            case TrangThaiHocTap.BuocThoiHoc:
            case TrangThaiHocTap.DaThoiHoc:
            case TrangThaiHocTap.DaTotNghiep:
                return KetQuaLuat.KhongQua(MaLuat.G2, $"Trạng thái học tập {hoSo.TrangThaiHocTap} không được giao ĐATN.", "A12");
        }

        return ngayXet > hoSo.HanToiDa
            ? KetQuaLuat.KhongQua(MaLuat.G2, $"Quá thời gian học tối đa (hạn {hoSo.HanToiDa:dd/MM/yyyy}).", "A12")
            : KetQuaLuat.Qua(MaLuat.G2, "Trạng thái học tập hợp lệ, trong thời gian học tối đa.");
    }
}
