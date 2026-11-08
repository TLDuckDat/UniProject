using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>Điểm quá trình: GVHD, GVĐD và điểm khác (chỉ dùng khi bộ môn tách điểm khác – F02).</summary>
public sealed record DiemThanhPhan(decimal DiemGVHD, decimal DiemGVDD, decimal? DiemKhac = null);

/// <summary>
/// Một thành viên HĐ (sau khi đã thay thế) kèm phiếu điểm của họ cho SV đang xét.
/// <paramref name="Diem"/> null = chưa nộp phiếu; <paramref name="DongYTrucTuyen"/> chỉ dùng khi HĐ trực tuyến.
/// </summary>
public sealed record ThanhVienChamDiem(
    long ThanhVienId,
    ChucDanhHoiDong ChucDanh,
    bool CoMat,
    decimal? Diem = null,
    bool? DongYTrucTuyen = null);

/// <summary>Hội đồng tại buổi bảo vệ của một SV.</summary>
public sealed record HoiDongChamDiem(HinhThucBaoVe HinhThuc, string? LinkGhiHinh, IReadOnlyList<ThanhVienChamDiem> ThanhVien);

/// <summary>Kết quả điểm ĐATN – khớp các cột của bảng <c>DiemDATN</c>.</summary>
public sealed record KetQuaDiemDATN(
    decimal DiemQuaTrinh,
    decimal DiemBaoVe,
    decimal DiemDATN,
    string DiemChu,
    decimal DiemHe4,
    decimal TrongSoQT,
    decimal TrongSoHD,
    decimal DiemDatDATN,
    decimal ChenhLechMax,
    bool CanhBaoChenhLech) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.DiemDATN;

    public bool Dat => DiemDATN >= DiemDatDATN;

    public string LyDo =>
        $"QT {DiemQuaTrinh:0.0} × {TrongSoQT:0.00} + BV {DiemBaoVe:0.0} × {TrongSoHD:0.00} = {DiemDATN:0.0} ({DiemChu}) – "
        + (Dat ? "ĐẠT." : $"KHÔNG ĐẠT (cần ≥ {DiemDatDATN:0.0}).")
        + (CanhBaoChenhLech ? $" Cảnh báo: điểm thành viên HĐ chênh {ChenhLechMax:0.0}, cần Chủ tịch xác nhận." : string.Empty);

    /// <summary>F03: 4,0–5,4 (D, D+) – học phần thường đạt nhưng ĐATN KHÔNG ĐẠT; E10: chênh lệch lớn.</summary>
    public string? MaNgoaiLe => !Dat && DiemDATN >= 4.0m ? "F03" : CanhBaoChenhLech ? "E10" : null;
}

/// <summary>Công thức điểm ĐATN (đặc tả mục 8) và điều kiện mở phiên / chốt điểm (mục 5, E01, E02, E08, E10, F04).</summary>
public static class TinhDiemDATN
{
    /// <summary>
    /// QT = round1(TBC(GVHD, GVĐD, [Khác nếu tách])); BV = round1(TBC phiếu của TV CÓ MẶT);
    /// ĐATN = round1(QT × a + BV × b); Đạt khi ĐATN ≥ DiemDatDATN.
    /// </summary>
    public static KetQuaDiemDATN Tinh(DiemThanhPhan diem, IReadOnlyList<ThanhVienChamDiem> thanhVien, ThamSoTinhDiem thamSo)
    {
        ArgumentNullException.ThrowIfNull(diem);
        ArgumentNullException.ThrowIfNull(thanhVien);
        ArgumentNullException.ThrowIfNull(thamSo);

        var thanhPhan = new List<decimal> { diem.DiemGVHD, diem.DiemGVDD };
        if (thamSo.TachDiemKhac)
        {
            thanhPhan.Add(diem.DiemKhac
                ?? throw new DomainException("UPM_F02_THIEU_DIEM_KHAC", "Bộ môn tách điểm khác nhưng chưa có điểm khác."));
        }

        var coMat = thanhVien.Where(t => t.CoMat).ToList();
        if (coMat.Count == 0)
        {
            throw new DomainException("UPM_E02_KHONG_CO_THANH_VIEN", "Không có thành viên Hội đồng có mặt.");
        }

        if (coMat.Any(t => t.Diem is null))
        {
            throw new DomainException("UPM_F04_THIEU_PHIEU_DIEM", "Còn thành viên có mặt chưa nộp phiếu điểm.");
        }

        var phieu = coMat.Select(t => t.Diem!.Value).ToList();
        foreach (var d in thanhPhan.Concat(phieu))
        {
            KiemTraDiem.Thang10(d, "thành phần");
        }

        var qt = LamTron.Diem(thanhPhan.Average());
        var bv = LamTron.Diem(phieu.Average());
        var quyDoi = QuyDoiDiem.TuThang10(qt * thamSo.TrongSoQT + bv * thamSo.TrongSoHD);
        var chenhLech = phieu.Max() - phieu.Min();

        return new KetQuaDiemDATN(qt, bv, quyDoi.Diem10, quyDoi.DiemChu, quyDoi.Diem4, thamSo.TrongSoQT, thamSo.TrongSoHD,
            thamSo.DiemDatDATN, chenhLech, chenhLech >= thamSo.NguongChenhLech);
    }

    /// <summary>
    /// Số ủy viên có mặt tối thiểu: HĐ 3 → 1, HĐ 5 → 2 (đặc tả mục 5). Tổng quát (n − 1) / 2 làm tròn xuống,
    /// nên HĐ 4 → 1 (chưa có quy định – xem Câu hỏi cho BA Phần 4).
    /// </summary>
    public static int SoUyVienToiThieu(int soThanhVien) => (soThanhVien - 1) / 2;

    /// <summary>T14, E01, E02: số thành viên trong [Min, Max]; có mặt CT + TK + đủ số UV tối thiểu.</summary>
    public static KetQuaTongHop KiemTraMoPhien(HoiDongChamDiem hoiDong, ThamSoQuyChe ruleSet)
    {
        ArgumentNullException.ThrowIfNull(hoiDong);
        ArgumentNullException.ThrowIfNull(ruleSet);
        return new KetQuaTongHop(MaLuat.MoPhien, [KiemTraSoThanhVien(hoiDong, ruleSet), KiemTraQuorum(hoiDong)]);
    }

    /// <summary>
    /// Điều kiện chốt điểm: mở phiên hợp lệ + mọi TV có mặt đã nộp phiếu (F04) + trực tuyến đủ ≥ 3 TV có mặt,
    /// tất cả đồng ý, có link ghi hình (E08, T15) + chênh lệch ≥ ngưỡng thì Chủ tịch phải xác nhận (E10, ⚠O9).
    /// </summary>
    public static KetQuaTongHop KiemTraChotDiem(HoiDongChamDiem hoiDong, ThamSoQuyChe ruleSet, ThamSoTinhDiem thamSo,
        bool chuTichDaXacNhanChenhLech)
    {
        ArgumentNullException.ThrowIfNull(hoiDong);
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(thamSo);

        var coMat = hoiDong.ThanhVien.Where(t => t.CoMat).ToList();
        var thieuPhieu = coMat.Count(t => t.Diem is null);
        var duPhieu = thieuPhieu == 0
            ? KetQuaLuat.Qua(MaLuat.DuPhieu, "Mọi thành viên có mặt đã nộp phiếu điểm.")
            : KetQuaLuat.KhongQua(MaLuat.DuPhieu, $"Còn {thieuPhieu} thành viên có mặt chưa nộp phiếu điểm.", "F04");

        var luat = new List<KetQuaLuat> { KiemTraSoThanhVien(hoiDong, ruleSet), KiemTraQuorum(hoiDong), duPhieu };
        if (hoiDong.HinhThuc == HinhThucBaoVe.TrucTuyen)
        {
            luat.Add(KiemTraTrucTuyen(hoiDong, coMat));
        }

        var phieu = coMat.Where(t => t.Diem is not null).Select(t => t.Diem!.Value).ToList();
        var chenhLech = phieu.Count == 0 ? 0m : phieu.Max() - phieu.Min();
        luat.Add(chenhLech < thamSo.NguongChenhLech || chuTichDaXacNhanChenhLech
            ? KetQuaLuat.Qua(MaLuat.ChenhLech, $"Chênh lệch điểm thành viên {chenhLech:0.0}.")
            : KetQuaLuat.KhongQua(MaLuat.ChenhLech,
                $"Điểm thành viên chênh {chenhLech:0.0} ≥ {thamSo.NguongChenhLech:0.0}: cần Chủ tịch xác nhận.", "E10"));

        return new KetQuaTongHop(MaLuat.ChotDiem, luat);
    }

    private static KetQuaLuat KiemTraSoThanhVien(HoiDongChamDiem hoiDong, ThamSoQuyChe ruleSet)
    {
        var n = hoiDong.ThanhVien.Count;
        return n >= ruleSet.MinTVHoiDong && n <= ruleSet.MaxTVHoiDong
            ? KetQuaLuat.Qua(MaLuat.SoThanhVien, $"Hội đồng {n} thành viên.")
            : KetQuaLuat.KhongQua(MaLuat.SoThanhVien,
                $"Hội đồng {n} thành viên, phải từ {ruleSet.MinTVHoiDong} đến {ruleSet.MaxTVHoiDong}.", "E01");
    }

    private static KetQuaLuat KiemTraQuorum(HoiDongChamDiem hoiDong)
    {
        var coMat = hoiDong.ThanhVien.Where(t => t.CoMat).ToList();
        var coCT = coMat.Any(t => t.ChucDanh == ChucDanhHoiDong.ChuTich);
        var coTK = coMat.Any(t => t.ChucDanh == ChucDanhHoiDong.ThuKy);
        var soUV = coMat.Count(t => t.ChucDanh == ChucDanhHoiDong.UyVien);
        var canUV = SoUyVienToiThieu(hoiDong.ThanhVien.Count);

        return coCT && coTK && soUV >= canUV
            ? KetQuaLuat.Qua(MaLuat.Quorum, $"Có mặt Chủ tịch, Thư ký và {soUV} ủy viên.")
            : KetQuaLuat.KhongQua(MaLuat.Quorum,
                $"Không đủ số người có mặt: cần Chủ tịch + Thư ký + {canUV} ủy viên "
                + $"(hiện: Chủ tịch {(coCT ? "có" : "vắng")}, Thư ký {(coTK ? "có" : "vắng")}, {soUV} ủy viên).", "E02");
    }

    private static KetQuaLuat KiemTraTrucTuyen(HoiDongChamDiem hoiDong, List<ThanhVienChamDiem> coMat)
    {
        var loi = new List<string>();
        if (coMat.Count < 3)
        {
            loi.Add($"chỉ có {coMat.Count} thành viên trực tuyến (cần ≥ 3)");
        }

        var chuaDongY = coMat.Count(t => t.DongYTrucTuyen != true);
        if (chuaDongY > 0)
        {
            loi.Add($"{chuaDongY} thành viên chưa đồng ý hình thức trực tuyến");
        }

        if (string.IsNullOrWhiteSpace(hoiDong.LinkGhiHinh))
        {
            loi.Add("chưa có link ghi hình");
        }

        return loi.Count == 0
            ? KetQuaLuat.Qua(MaLuat.TrucTuyen, "Bảo vệ trực tuyến hợp lệ.")
            : KetQuaLuat.KhongQua(MaLuat.TrucTuyen, $"Bảo vệ trực tuyến chưa hợp lệ: {string.Join("; ", loi)}.", "E08");
    }
}
