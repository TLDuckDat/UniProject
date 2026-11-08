using Upm.Domain.Common;
using Upm.Domain.Entities;

namespace Upm.Domain.Rules;

/// <summary>
/// Tham số quy chế đọc từ <see cref="RuleSet"/> (CLAUDE.md §5.4 – không hard-code ngưỡng).
/// <see cref="MaRuleSet"/> chính là <c>RuleVersion</c> lưu vào snapshot.
/// </summary>
public sealed record ThamSoQuyChe
{
    public required string MaRuleSet { get; init; }

    /// <summary>Mặc định 10 – đạt khi TC chưa tích lũy ≤ giá trị này (A03).</summary>
    public required int MaxTCChuaTichLuy { get; init; }

    /// <summary>1,90 – so với GPA đã làm tròn 2 chữ số (K5).</summary>
    public required decimal GpaBaoVeMin { get; init; }

    /// <summary>2,00.</summary>
    public required decimal GpaTotNghiepMin { get; init; }

    /// <summary>5,5 – điểm ĐATN đạt (F03).</summary>
    public required decimal DiemDatDATN { get; init; }

    public required int MinTVHoiDong { get; init; }

    public required int MaxTVHoiDong { get; init; }

    /// <summary>⚠O6: UTC-2021 = 5 (%), UTC-2026 = 10 (%). Hạ hạng khi tỉ lệ TC học lại VƯỢT ngưỡng.</summary>
    public required decimal NguongHaHangHocLaiPct { get; init; }

    /// <summary>Hạ hạng khi có kỷ luật từ mức cảnh cáo trở lên (UTC-2021).</summary>
    public required bool HaHangKhiKyLuat { get; init; }

    /// <summary>
    /// Hạ hạng khi vi phạm liêm chính học thuật (UTC-2026 / TT 56). RuleSet CHƯA có cột này – xem Nợ kỹ thuật Phần 4.
    /// </summary>
    public bool HaHangKhiViPhamLiemChinh { get; init; }

    public static ThamSoQuyChe Tu(RuleSet ruleSet, bool haHangKhiViPhamLiemChinh = false)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        return new ThamSoQuyChe
        {
            MaRuleSet = ruleSet.MaRuleSet,
            MaxTCChuaTichLuy = ruleSet.MaxTCChuaTichLuy,
            GpaBaoVeMin = ruleSet.GpaBaoVeMin,
            GpaTotNghiepMin = ruleSet.GpaTotNghiepMin,
            DiemDatDATN = ruleSet.DiemDatDATN,
            MinTVHoiDong = ruleSet.MinTVHoiDong,
            MaxTVHoiDong = ruleSet.MaxTVHoiDong,
            NguongHaHangHocLaiPct = ruleSet.NguongHaHangHocLaiPct,
            HaHangKhiKyLuat = ruleSet.HaHangKhiKyLuat,
            HaHangKhiViPhamLiemChinh = haHangKhiViPhamLiemChinh,
        };
    }
}

/// <summary>Tham số tính điểm ĐATN: trọng số theo RuleSet × Bộ môn (⚠O1), tách điểm khác (F02), ngưỡng chênh lệch (⚠O9).</summary>
public sealed record ThamSoTinhDiem
{
    /// <summary>Ngưỡng cảnh báo chênh lệch điểm giữa thành viên HĐ khi chưa có cấu hình (⚠O9, E10).</summary>
    public const decimal NguongChenhLechMacDinh = 2.0m;

    public ThamSoTinhDiem(decimal trongSoQT, decimal trongSoHD, bool tachDiemKhac, decimal diemDatDATN,
        decimal nguongChenhLech = NguongChenhLechMacDinh)
    {
        if (trongSoQT is < 0m or > 1m || trongSoHD is < 0m or > 1m || trongSoQT + trongSoHD != 1m)
        {
            throw new DomainException("UPM_F01_TRONG_SO_KHONG_HOP_LE",
                $"Trọng số quá trình/hội đồng {trongSoQT}/{trongSoHD} không hợp lệ (mỗi trọng số 0–1, tổng bằng 1).");
        }

        TrongSoQT = trongSoQT;
        TrongSoHD = trongSoHD;
        TachDiemKhac = tachDiemKhac;
        DiemDatDATN = diemDatDATN;
        NguongChenhLech = nguongChenhLech;
    }

    public decimal TrongSoQT { get; }

    public decimal TrongSoHD { get; }

    public bool TachDiemKhac { get; }

    public decimal DiemDatDATN { get; }

    public decimal NguongChenhLech { get; }

    /// <summary>
    /// Bộ môn có <see cref="ThamSoBoMon"/> cho RuleSet thì dùng trọng số của bộ môn; không có thì dùng
    /// <c>RuleSet.TrongSoQT</c> và HĐ = 1 − QT, không tách điểm khác.
    /// </summary>
    public static ThamSoTinhDiem Tu(RuleSet ruleSet, ThamSoBoMon? thamSoBoMon,
        decimal nguongChenhLech = NguongChenhLechMacDinh)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        return thamSoBoMon is null
            ? new ThamSoTinhDiem(ruleSet.TrongSoQT, 1m - ruleSet.TrongSoQT, false, ruleSet.DiemDatDATN, nguongChenhLech)
            : new ThamSoTinhDiem(thamSoBoMon.TrongSoQT, thamSoBoMon.TrongSoHD, thamSoBoMon.TachDiemKhac,
                ruleSet.DiemDatDATN, nguongChenhLech);
    }
}

/// <summary>Một dòng <c>RuleSetKhoaHoc</c>: khóa [Từ, Đến] áp RuleSet nào.</summary>
public sealed record AnhXaRuleSetKhoa(string MaRuleSet, int KhoaHocTu, int? KhoaHocDen, bool CanXacNhan = false)
{
    public bool ApDungCho(int khoaHoc) => khoaHoc >= KhoaHocTu && (KhoaHocDen is null || khoaHoc <= KhoaHocDen);
}

/// <summary>Kết quả chọn RuleSet theo khóa SV.</summary>
public sealed record KetQuaChonRuleSet(int KhoaHoc, string MaRuleSet, bool CanXacNhan) : IKetQuaLuat
{
    public string MaLuat => Rules.MaLuat.RuleSetTheoKhoa;

    public bool Dat => true;

    public string LyDo => CanXacNhan
        ? $"Khóa K{KhoaHoc} tạm áp {MaRuleSet}, cần xác nhận quy chế."
        : $"Khóa K{KhoaHoc} áp {MaRuleSet}.";

    /// <summary>A16: K66 chưa rõ quy chế; A15: các khóa khác áp theo khóa SV.</summary>
    public string? MaNgoaiLe => CanXacNhan ? "A16" : "A15";
}

/// <summary>RuleSet gắn theo KHÓA sinh viên, không theo đợt (CLAUDE.md §5.2, T17).</summary>
public static class ChonRuleSet
{
    public static KetQuaChonRuleSet TheoKhoa(int khoaHoc, IEnumerable<AnhXaRuleSetKhoa> bang)
    {
        ArgumentNullException.ThrowIfNull(bang);
        var khop = bang.Where(a => a.ApDungCho(khoaHoc)).ToList();
        return khop.Count switch
        {
            1 => new KetQuaChonRuleSet(khoaHoc, khop[0].MaRuleSet, khop[0].CanXacNhan),
            0 => throw new DomainException("UPM_RULESET_KHONG_XAC_DINH", $"Chưa cấu hình RuleSet cho khóa K{khoaHoc}."),
            _ => throw new DomainException("UPM_RULESET_KHONG_XAC_DINH",
                $"Khóa K{khoaHoc} khớp nhiều RuleSet: {string.Join(", ", khop.Select(k => k.MaRuleSet))}."),
        };
    }
}
