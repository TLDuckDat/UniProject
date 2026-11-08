using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>Kết luận xét bảo vệ (bảng quyết định mục 7). Tên = trạng thái đích của <c>SVTrongDot</c>.</summary>
public enum KetLuanXetBaoVe
{
    DUOC_BAO_VE,
    HOAN_BAO_VE,
    PHAI_LAM_LAI,
    YEU_CAU_CHINH_SUA,
}

/// <summary>Hồ sơ SV đầu vào xét bảo vệ (đọc từ YKienGVHD, KetQuaDocDuyet, BangDiem, KyLuatSV).</summary>
public sealed record HoSoXetBaoVe
{
    public required IReadOnlyList<KyLuat> KyLuat { get; init; }

    /// <summary>K2.</summary>
    public required bool GVHDDongY { get; init; }

    /// <summary>Lý do GVHD không đồng ý – bắt buộc khi <see cref="GVHDDongY"/> = false (D05).</summary>
    public string? LyDoGVHD { get; init; }

    /// <summary>K3 – kết quả đọc duyệt mới nhất của GVĐD.</summary>
    public required KetQuaDocDuyetLoai KetQuaGVDD { get; init; }

    /// <summary>Đã dùng hết vòng sửa gấp (đã đọc lại 1 lần hoặc nộp bản sửa quá hạn) – D08.</summary>
    public bool DaHetVongSuaGap { get; init; }

    public required DuLieuHocTapSV HocTap { get; init; }

    /// <summary>GPA tích lũy THÔ từ QLĐT; luật tự làm tròn 2 chữ số (K5, D04).</summary>
    public required decimal GpaTichLuyTho { get; init; }
}

/// <summary>Kết quả xét bảo vệ: kết luận, K1–K6, hạn hoãn, chi tiết TC còn thiếu (snapshot XetDieuKien).</summary>
public sealed record KetQuaXetBaoVe(
    KetLuanXetBaoVe KetLuan,
    IReadOnlyList<KetQuaLuat> CacLuat,
    decimal GpaLamTron,
    IReadOnlyList<ChiTietHocPhanChuaTichLuy> TCConThieu,
    HocKy? HocKyHanHoan,
    DateOnly? NgayHanHoan,
    string RuleVersion) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.XetBaoVe;

    public bool Dat => KetLuan == KetLuanXetBaoVe.DUOC_BAO_VE;

    public string LyDo => KetLuan switch
    {
        KetLuanXetBaoVe.DUOC_BAO_VE => "Đủ điều kiện bảo vệ (K1–K6 đều đạt).",
        KetLuanXetBaoVe.HOAN_BAO_VE =>
            $"{LyDoTruot} Hoãn bảo vệ đến hết học kỳ {HocKyHanHoan} ({NgayHanHoan:dd/MM/yyyy}); quá hạn chuyển PHAI_LAM_LAI.",
        _ => LyDoTruot,
    };

    /// <summary>Mã ngoại lệ của luật đầu tiên quyết định kết luận (C06, D05, D06, D07, D01, D02, D03).</summary>
    public string? MaNgoaiLe => CacLuat.FirstOrDefault(l => !l.Dat)?.MaNgoaiLe;

    /// <summary>Tra kết quả một luật theo mã (K1…K6) – tiện lưu các cột K*_ của XetDieuKien.</summary>
    public KetQuaLuat this[string maLuat] => CacLuat.Single(l => l.MaLuat == maLuat);

    private string LyDoTruot => string.Join(" ", CacLuat.Where(l => !l.Dat).Select(l => l.LyDo));
}

/// <summary>Tình trạng bản sửa gấp sau đọc duyệt (D06, D08, T16).</summary>
public enum TinhTrangSuaGap
{
    /// <summary>Còn hạn, SV chưa nộp.</summary>
    ChoNop,

    /// <summary>SV nộp đúng hạn ở vòng 1 → GVĐD đọc lại.</summary>
    DuocDocLai,

    /// <summary>Quá hạn hoặc đã đọc lại 1 vòng → không mở vòng 2, GVĐD phải chọn đồng ý / không đồng ý.</summary>
    GVDDPhaiQuyetDinh,
}

public sealed record KetQuaSuaGap(TinhTrangSuaGap TinhTrang, DateOnly HanNop) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.SuaGap;

    public bool Dat => TinhTrang != TinhTrangSuaGap.GVDDPhaiQuyetDinh;

    public string LyDo => TinhTrang switch
    {
        TinhTrangSuaGap.ChoNop => $"Chờ SV nộp bản sửa (hạn {HanNop:dd/MM/yyyy}).",
        TinhTrangSuaGap.DuocDocLai => "Bản sửa nộp đúng hạn – GVĐD đọc lại.",
        _ => "Hết vòng sửa gấp: không mở vòng 2, GVĐD phải chọn đồng ý hoặc không đồng ý cho bảo vệ.",
    };

    public string? MaNgoaiLe => TinhTrang == TinhTrangSuaGap.GVDDPhaiQuyetDinh ? "D08" : "D06";
}

/// <summary>Luật xét bảo vệ K1–K6 + bảng quyết định (đặc tả mục 7).</summary>
public static class XetBaoVe
{
    /// <summary>Số vòng đọc lại tối đa sau sửa gấp (K3: "sau tối đa 1 vòng sửa gấp").</summary>
    public const int SoVongSuaGapToiDa = 1;

    /// <summary>
    /// Thứ tự quyết định: K1 ✘ / K2 ✘ / K3 ✘ → PHAI_LAM_LAI; K3 lỗi nhỏ → YEU_CAU_CHINH_SUA;
    /// K4/K5/K6 có ✘ → HOAN_BAO_VE (hạn = hết học kỳ liền kề); còn lại DUOC_BAO_VE.
    /// O3: chưa qua TTTN (K6) xử lý như K4 → HOAN_BAO_VE.
    /// </summary>
    public static KetQuaXetBaoVe Xet(HoSoXetBaoVe hoSo, ThamSoQuyChe ruleSet, HocKy kyXet, DateOnly ngayXet, ILichHocKy lichHocKy)
    {
        ArgumentNullException.ThrowIfNull(hoSo);
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(lichHocKy);

        if (!hoSo.GVHDDongY && string.IsNullOrWhiteSpace(hoSo.LyDoGVHD))
        {
            throw new DomainException("UPM_D05_THIEU_LY_DO_GVHD", "GVHD không đồng ý cho bảo vệ thì bắt buộc ghi lý do.");
        }

        if (hoSo.KetQuaGVDD == KetQuaDocDuyetLoai.SuaGap && hoSo.DaHetVongSuaGap)
        {
            throw new DomainException("UPM_D08_KHONG_MO_VONG_2",
                "Đã hết vòng sửa gấp: GVĐD phải chọn đồng ý hoặc không đồng ý cho bảo vệ.");
        }

        var k1 = XetGiao.XetKyLuat(MaLuat.K1, hoSo.KyLuat, ngayXet, "C06");
        var k2 = hoSo.GVHDDongY
            ? KetQuaLuat.Qua(MaLuat.K2, "GVHD đồng ý cho bảo vệ.")
            : KetQuaLuat.KhongQua(MaLuat.K2, $"GVHD không đồng ý: {hoSo.LyDoGVHD}", "D05");
        var k3 = hoSo.KetQuaGVDD switch
        {
            KetQuaDocDuyetLoai.DongY => KetQuaLuat.Qua(MaLuat.K3, "GVĐD đồng ý cho bảo vệ."),
            KetQuaDocDuyetLoai.SuaGap => KetQuaLuat.KhongQua(MaLuat.K3, "GVĐD yêu cầu sửa gấp lỗi nhỏ.", "D06"),
            _ => KetQuaLuat.KhongQua(MaLuat.K3, "GVĐD không đồng ý cho bảo vệ (lỗi bản chất / đạo văn nặng).", "D07"),
        };

        var conThieu = TCConThieuK4(hoSo.HocTap);
        var tongThieu = conThieu.Sum(c => c.SoTC);
        var k4 = tongThieu == 0
            ? KetQuaLuat.Qua(MaLuat.K4, "Đủ tín chỉ theo CTĐT.")
            : KetQuaLuat.KhongQua(MaLuat.K4,
                $"Còn thiếu {tongThieu} TC: {string.Join(", ", conThieu.Select(c => $"{c.MaHP} ({c.SoTC} TC)"))}.", "D01");

        var gpa = LamTron.Gpa(hoSo.GpaTichLuyTho);
        var k5 = gpa >= ruleSet.GpaBaoVeMin
            ? KetQuaLuat.Qua(MaLuat.K5, $"GPA tích lũy {gpa:0.00} ≥ {ruleSet.GpaBaoVeMin:0.00}.",
                hoSo.GpaTichLuyTho < ruleSet.GpaBaoVeMin ? "D04" : null)
            : KetQuaLuat.KhongQua(MaLuat.K5, $"GPA tích lũy {gpa:0.00} < {ruleSet.GpaBaoVeMin:0.00}.", "D02");

        var k6 = TinhTCChuaTichLuy.DaDatTTTN(hoSo.HocTap)
            ? KetQuaLuat.Qua(MaLuat.K6, "Đã đạt Thực tập tốt nghiệp.")
            : KetQuaLuat.KhongQua(MaLuat.K6, "Chưa đạt Thực tập tốt nghiệp.", "D03");

        var cacLuat = new[] { k1, k2, k3, k4, k5, k6 };
        KetLuanXetBaoVe ketLuan;
        if (!k1.Dat || !k2.Dat || hoSo.KetQuaGVDD == KetQuaDocDuyetLoai.KhongDongY)
        {
            ketLuan = KetLuanXetBaoVe.PHAI_LAM_LAI;
        }
        else if (!k3.Dat)
        {
            ketLuan = KetLuanXetBaoVe.YEU_CAU_CHINH_SUA;
        }
        else if (!k4.Dat || !k5.Dat || !k6.Dat)
        {
            ketLuan = KetLuanXetBaoVe.HOAN_BAO_VE;
        }
        else
        {
            ketLuan = KetLuanXetBaoVe.DUOC_BAO_VE;
        }

        HocKy? kyHan = ketLuan == KetLuanXetBaoVe.HOAN_BAO_VE ? kyXet.KeTiep() : null;
        DateOnly? ngayHan = kyHan is { } k ? lichHocKy.NgayKetThuc(k) : null;
        return new KetQuaXetBaoVe(ketLuan, cacLuat, gpa, conThieu, kyHan, ngayHan, ruleSet.MaRuleSet);
    }

    /// <summary>D01: HOAN_BAO_VE quá hạn (sau ngày kết thúc học kỳ liền kề) → job chuyển PHAI_LAM_LAI.</summary>
    public static KetQuaLuat KiemTraHanHoan(DateOnly ngayHanHoan, DateOnly homNay) =>
        homNay > ngayHanHoan
            ? KetQuaLuat.KhongQua(MaLuat.HetHanHoan, $"Đã quá hạn hoãn bảo vệ ({ngayHanHoan:dd/MM/yyyy}) → PHAI_LAM_LAI.", "D01")
            : KetQuaLuat.Qua(MaLuat.HetHanHoan, $"Còn trong hạn hoãn bảo vệ ({ngayHanHoan:dd/MM/yyyy}).", "D01");

    /// <summary>
    /// D06/D08/T16: hạn nộp bản sửa = ngày GVĐD yêu cầu + <paramref name="soNgaySuaGapToiDa"/>
    /// (<c>ThamSoBoMon.SoNgaySuaGapToiDa</c>, 1–3 ngày). Nộp đúng hạn ở vòng đầu → đọc lại; quá hạn hoặc đã đọc lại → GVĐD
    /// phải quyết định.
    /// </summary>
    public static KetQuaSuaGap KiemTraSuaGap(DateOnly ngayYeuCau, int soNgaySuaGapToiDa, DateOnly? ngayNopBanSua,
        DateOnly homNay, int soVongDaDocLai)
    {
        if (soNgaySuaGapToiDa is < 1 or > 3)
        {
            throw new DomainException("UPM_D06_SO_NGAY_SUA_GAP_KHONG_HOP_LE", "Số ngày sửa gấp phải từ 1 đến 3.");
        }

        var han = ngayYeuCau.AddDays(soNgaySuaGapToiDa);
        TinhTrangSuaGap tinhTrang;
        if (soVongDaDocLai >= SoVongSuaGapToiDa)
        {
            tinhTrang = TinhTrangSuaGap.GVDDPhaiQuyetDinh;
        }
        else if (ngayNopBanSua is { } nop)
        {
            tinhTrang = nop <= han ? TinhTrangSuaGap.DuocDocLai : TinhTrangSuaGap.GVDDPhaiQuyetDinh;
        }
        else
        {
            tinhTrang = homNay <= han ? TinhTrangSuaGap.ChoNop : TinhTrangSuaGap.GVDDPhaiQuyetDinh;
        }

        return new KetQuaSuaGap(tinhTrang, han);
    }

    /// <summary>
    /// K4: học phần tính điểm trong CTĐT chưa tích lũy, KHÔNG tính GDQP, GDTC, ĐATN, học phần thay thế ĐATN.
    /// TTTN được xét riêng ở K6 để lý do rõ ràng (D03 thay vì D01) – kết luận vẫn là HOAN_BAO_VE như nhau.
    /// </summary>
    private static List<ChiTietHocPhanChuaTichLuy> TCConThieuK4(DuLieuHocTapSV hocTap) =>
        TinhTCChuaTichLuy.ChuaTichLuy(hocTap)
            .Where(x => x.HocPhan.Loai == LoaiHocPhan.Thuong && !x.HocPhan.LaHPThayTheDATN)
            .Select(x => TinhTCChuaTichLuy.TaoChiTiet(x.HocPhan, x.LoaiNo, false, "Chưa tích lũy.", "D01"))
            .ToList();
}
