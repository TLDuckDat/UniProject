using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>Một học phần chưa tích lũy – tương ứng một dòng <c>XetDieuKienChiTiet</c>.</summary>
public sealed record ChiTietHocPhanChuaTichLuy(
    long HocPhanId,
    string MaHP,
    int SoTC,
    LoaiHocPhan LoaiHP,
    LoaiNo LoaiNo,
    bool BiLoaiTru,
    string LyDo,
    string? MaNgoaiLe);

/// <summary>Kết quả tính TC chưa tích lũy (mục 6, G3). <see cref="Dat"/> khi tổng ≤ MaxTCChuaTichLuy.</summary>
public sealed record KetQuaTCChuaTichLuy(
    int TongTC,
    int MaxTC,
    bool DungTienDo,
    bool DaDatTTTN,
    IReadOnlyList<ChiTietHocPhanChuaTichLuy> ChiTiet,
    string RuleVersion) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.TCChuaTichLuy;

    public bool Dat => TongTC <= MaxTC;

    public string LyDo => Dat
        ? $"Tín chỉ chưa tích lũy (đã loại trừ) = {TongTC} ≤ {MaxTC}."
        : $"Nợ {TongTC} TC > {MaxTC}: {string.Join(", ", ChiTiet.Where(c => !c.BiLoaiTru).Select(c => $"{c.MaHP} ({c.SoTC} TC)"))}.";

    /// <summary>A02: vượt ngưỡng; A03: đúng bằng ngưỡng; A01: còn nợ nhưng trong ngưỡng.</summary>
    public string? MaNgoaiLe => TongTC > MaxTC ? "A02" : TongTC == MaxTC ? "A03" : TongTC > 0 ? "A01" : null;
}

/// <summary>
/// Tính TC chưa tích lũy theo pseudo-code đặc tả mục 6:
/// loại trừ GDQP, GDTC, ĐATN (A05); TTTN loại trừ nếu đúng tiến độ (A06) hoặc đã đạt TTTN; trễ tiến độ & chưa có
/// điểm TTTN → cộng (A07). Đã tích lũy = có lần học Đạt (≥ D) hoặc R (A09); đang học (A04), I, X (A08) = CHƯA.
/// </summary>
public static class TinhTCChuaTichLuy
{
    public static KetQuaTCChuaTichLuy Tinh(DuLieuHocTapSV svData, ThamSoQuyChe ruleSet, HocKy kyXet)
    {
        ArgumentNullException.ThrowIfNull(svData);
        ArgumentNullException.ThrowIfNull(ruleSet);

        // Xét đầu kỳ cuối theo kế hoạch chuẩn: kỳ xét không muộn hơn kỳ cuối kế hoạch = đúng tiến độ.
        var dungTienDo = kyXet <= svData.KyCuoiKeHoach;
        var daDatTTTN = DaDatTTTN(svData);

        var chiTiet = ChuaTichLuy(svData).Select(x => PhanLoaiG3(x.HocPhan, x.LoaiNo, dungTienDo)).ToList();
        var tong = chiTiet.Where(c => !c.BiLoaiTru).Sum(c => c.SoTC);
        return new KetQuaTCChuaTichLuy(tong, ruleSet.MaxTCChuaTichLuy, dungTienDo, daDatTTTN, chiTiet, ruleSet.MaRuleSet);
    }

    /// <summary>CTĐT không có học phần TTTN coi như không yêu cầu TTTN.</summary>
    internal static bool DaDatTTTN(DuLieuHocTapSV svData) =>
        svData.HocPhanCTDT.Where(h => h.Loai == LoaiHocPhan.TTTN).All(h => DaTichLuy(svData, h.HocPhanId));

    /// <summary>Các học phần tính điểm trong CTĐT chưa tích lũy, kèm loại nợ theo bảng điểm.</summary>
    internal static IEnumerable<(HocPhanCTDT HocPhan, LoaiNo LoaiNo)> ChuaTichLuy(DuLieuHocTapSV svData) =>
        svData.HocPhanCTDT
            .Where(h => h.TinhDiem && !DaTichLuy(svData, h.HocPhanId))
            .Select(h => (h, LoaiNoTheoBangDiem(svData, h.HocPhanId)));

    private static bool DaTichLuy(DuLieuHocTapSV svData, long hocPhanId) =>
        svData.BangDiem.Any(d => d.HocPhanId == hocPhanId && d.TrangThai is TrangThaiDiem.Dat or TrangThaiDiem.R);

    private static LoaiNo LoaiNoTheoBangDiem(DuLieuHocTapSV svData, long hocPhanId)
    {
        var lanHoc = svData.BangDiem.Where(d => d.HocPhanId == hocPhanId).ToList();
        if (lanHoc.Any(d => d.TrangThai == TrangThaiDiem.DangHoc))
        {
            return LoaiNo.DangHoc;
        }

        return lanHoc.OrderByDescending(d => d.LanHoc).FirstOrDefault()?.TrangThai switch
        {
            TrangThaiDiem.I => LoaiNo.DiemI,
            TrangThaiDiem.X => LoaiNo.DiemX,
            _ => LoaiNo.No,
        };
    }

    private static ChiTietHocPhanChuaTichLuy PhanLoaiG3(HocPhanCTDT hp, LoaiNo loaiNo, bool dungTienDo)
    {
        if (hp.Loai is LoaiHocPhan.GDQP or LoaiHocPhan.GDTC or LoaiHocPhan.DATN)
        {
            return TaoChiTiet(hp, loaiNo, true, $"{hp.Loai} không tính vào giới hạn.", "A05");
        }

        if (hp.LaHPThayTheDATN)
        {
            return TaoChiTiet(hp, loaiNo, true, "Học phần thay thế ĐATN – không bắt buộc khi làm ĐATN.", null);
        }

        if (hp.Loai == LoaiHocPhan.TTTN)
        {
            // Đã đạt TTTN thì không vào danh sách chưa tích lũy, nên ở đây chỉ còn: đúng tiến độ (loại trừ) hoặc trễ (cộng).
            return dungTienDo
                ? TaoChiTiet(hp, loaiNo, true, "Đúng tiến độ: TTTN học song song đầu kỳ, không cộng.", "A06")
                : TaoChiTiet(hp, LoaiNo.TTTNCong, false, "Trễ tiến độ, chưa có điểm TTTN: cộng TC TTTN.", "A07");
        }

        return loaiNo switch
        {
            LoaiNo.DangHoc => TaoChiTiet(hp, loaiNo, false, "Đang học trong kỳ, chưa có điểm.", "A04"),
            LoaiNo.DiemI or LoaiNo.DiemX => TaoChiTiet(hp, loaiNo, false, $"Điểm {(loaiNo == LoaiNo.DiemI ? "I" : "X")} – chưa tích lũy.", "A08"),
            _ => TaoChiTiet(hp, loaiNo, false, "Nợ học phần.", null),
        };
    }

    internal static ChiTietHocPhanChuaTichLuy TaoChiTiet(HocPhanCTDT hp, LoaiNo loaiNo, bool biLoaiTru, string lyDo, string? ma) =>
        new(hp.HocPhanId, hp.MaHP, hp.SoTC, hp.Loai, loaiNo, biLoaiTru, lyDo, ma);
}
