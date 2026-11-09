using System.Globalization;
using System.Text;
using Upm.Domain.Enums;

namespace Upm.Domain.Rules;

/// <summary>
/// Luật đề tài & phân công (đặc tả mục 5, B01–B11). Hàm thuần, không EF/DB; Application dựng dữ liệu đầu vào.
/// Mã ngoại lệ B01…B11 gắn vào <see cref="KetQuaLuat.MaNgoaiLe"/> để UI giải thích.
/// </summary>
public static class DeTaiRules
{
    public const int MaxSVToiDa = 3;

    /// <summary>Ngưỡng độ tương đồng (Dice, bigram) coi là trùng đề tài (B10).</summary>
    public const double NguongTrung = 0.85;

    /// <summary>Tên trùng đủ mạnh mới xét mô tả (tránh báo trùng vì mô tả chung chung).</summary>
    public const double NguongTenKemMoTa = 0.6;

    /// <summary>B01/B02: mặc định 1 SV; 2–3 SV chỉ với đề tài chế tạo / hệ thống (Bộ môn duyệt ở bước đề tài).</summary>
    public static KetQuaLuat KiemTraSoSV(LoaiDeTai loai, int maxSV)
    {
        if (maxSV is < 1 or > MaxSVToiDa)
            return KetQuaLuat.KhongQua(MaLuat.DeTaiSoSV, $"Số SV tối đa phải từ 1 đến {MaxSVToiDa}.", "B01");
        if (maxSV == 1)
            return KetQuaLuat.Qua(MaLuat.DeTaiSoSV, "Đề tài 1 SV (mặc định).", "B01");
        if (loai == LoaiDeTai.LyThuyet)
            return KetQuaLuat.KhongQua(MaLuat.DeTaiSoSV,
                "Đề tài lý thuyết chỉ dành cho 1 SV; nhóm 2–3 SV chỉ với đề tài chế tạo hoặc hệ thống.", "B02");
        return KetQuaLuat.Qua(MaLuat.DeTaiSoSV, $"Nhóm {maxSV} SV: cần Bộ môn duyệt trước khi SV đăng ký.", "B02");
    }

    /// <summary>B03: GV chưa vượt quota (SV/đợt) thì được nhận thêm; vượt → cần Trưởng BM duyệt ngoại lệ (có log).</summary>
    public static KetQuaLuat KiemTraQuota(int dangCo, int quota, bool coDuyetNgoaiLe)
    {
        if (dangCo + 1 <= quota)
            return KetQuaLuat.Qua(MaLuat.PhanCongQuota, $"Còn quota: {dangCo + 1}/{quota} SV.", "B03");
        if (coDuyetNgoaiLe)
            return KetQuaLuat.Qua(MaLuat.PhanCongQuota,
                $"Vượt quota ({dangCo + 1}/{quota}) – Trưởng Bộ môn đã duyệt ngoại lệ (có ghi log).", "B03");
        return KetQuaLuat.KhongQua(MaLuat.PhanCongQuota,
            $"Giảng viên đã đủ quota {dangCo}/{quota} SV trong đợt; cần Trưởng Bộ môn duyệt ngoại lệ.", "B03");
    }

    /// <summary>B06: chặn cứng GVĐD trùng GVHD (và đồng HD trùng GVHD/GVĐD).</summary>
    public static KetQuaLuat KiemTraTrungVaiTro(long gvhdId, long? gvddId, IEnumerable<long> dongHdIds)
    {
        if (gvddId == gvhdId)
            return KetQuaLuat.KhongQua(MaLuat.PhanCongGVDD, "GVĐD không được trùng GVHD trên cùng một sinh viên.", "B06");
        if (dongHdIds.Any(x => x == gvhdId || (gvddId is { } d && x == d)))
            return KetQuaLuat.KhongQua(MaLuat.PhanCongGVDD, "Đồng hướng dẫn không được trùng GVHD hoặc GVĐD.", "B06");
        return KetQuaLuat.Qua(MaLuat.PhanCongGVDD, "GVHD, GVĐD và đồng HD là ba người khác nhau.", "B06");
    }

    /// <summary>B07: GV đã tự khai báo xung đột lợi ích với SV này thì hệ thống chặn phân công.</summary>
    public static KetQuaLuat KiemTraXungDot(IEnumerable<long> gvDuocPhanCong, IReadOnlySet<long> gvKhaiBaoXungDot)
    {
        var trung = gvDuocPhanCong.Where(gvKhaiBaoXungDot.Contains).ToArray();
        return trung.Length == 0
            ? KetQuaLuat.Qua(MaLuat.PhanCongXungDot, "Không có xung đột lợi ích đã khai báo.", "B07")
            : KetQuaLuat.KhongQua(MaLuat.PhanCongXungDot,
                "Giảng viên đã khai báo xung đột lợi ích với sinh viên này.", "B07");
    }

    /// <summary>B11: GV ngoài trường / doanh nghiệp chỉ được tham gia khi có GV cơ hữu cùng đứng tên.</summary>
    public static KetQuaLuat KiemTraGVNgoaiTruong(bool coGVNgoaiTruong, bool coGVCoHuu)
    {
        if (!coGVNgoaiTruong)
            return KetQuaLuat.Qua(MaLuat.PhanCongNgoaiTruong, "Không có GV ngoài trường.", "B11");
        return coGVCoHuu
            ? KetQuaLuat.Qua(MaLuat.PhanCongNgoaiTruong, "GV ngoài trường có GV cơ hữu cùng đứng tên.", "B11")
            : KetQuaLuat.KhongQua(MaLuat.PhanCongNgoaiTruong,
                "GV ngoài trường phải có GV cơ hữu cùng đứng tên (GVHD hoặc GVĐD).", "B11");
    }

    /// <summary>
    /// B09: đổi tên / nội dung đề tài chỉ trước hạn (mốc tuần 3–4 – tính theo <paramref name="hanDoi"/>).
    /// </summary>
    public static KetQuaLuat KiemTraHanDoiDeTai(DateOnly homNay, DateOnly hanDoi) =>
        homNay <= hanDoi
            ? KetQuaLuat.Qua(MaLuat.HanDoiDeTai, $"Còn hạn đổi đề tài đến {hanDoi:dd/MM/yyyy}.", "B09")
            : KetQuaLuat.KhongQua(MaLuat.HanDoiDeTai,
                $"Đã quá hạn đổi tên / nội dung đề tài ({hanDoi:dd/MM/yyyy}).", "B09");

    /// <summary>B05: GV có tải thấp nhất (DangCo/Quota) còn chỗ; hòa thì lấy Id nhỏ. Chỉ gợi ý – người duyệt xác nhận.</summary>
    public static long? GoiYGiangVien(IEnumerable<TaiGiangVien> cacGiangVien)
    {
        return cacGiangVien
            .Where(x => x.Quota > 0 && x.DangCo < x.Quota)
            .OrderBy(x => (decimal)x.DangCo / x.Quota)
            .ThenBy(x => x.GiangVienId)
            .Select(x => (long?)x.GiangVienId)
            .FirstOrDefault();
    }

    /// <summary>Mức tải hiển thị: Thấp &lt; 80% · Cao 80–99% · Đủ quota = 100% · Vượt quota &gt; 100%.</summary>
    public static MucTaiGiangVien MucTai(int dangCo, int quota)
    {
        if (quota <= 0) return dangCo > 0 ? MucTaiGiangVien.VuotQuota : MucTaiGiangVien.Thap;
        if (dangCo > quota) return MucTaiGiangVien.VuotQuota;
        if (dangCo == quota) return MucTaiGiangVien.DuQuota;
        return (decimal)dangCo / quota >= 0.8m ? MucTaiGiangVien.Cao : MucTaiGiangVien.Thap;
    }

    /// <summary>
    /// B10: tìm đề tài các đợt khác có tên (hoặc mô tả, khi tên đã khá giống) trùng sau khi chuẩn hóa không dấu.
    /// </summary>
    public static IReadOnlyList<DeTaiTrung> TimTrung(
        string ten, string? moTa, IEnumerable<(long Id, string Ten, string? MoTa)> deTaiDotKhac)
    {
        var tenChuan = ChuanHoa(ten);
        var moTaChuan = ChuanHoa(moTa);
        var ketQua = new List<DeTaiTrung>();
        foreach (var x in deTaiDotKhac)
        {
            var tenKhac = ChuanHoa(x.Ten);
            var tenSim = DoTuongDong(tenChuan, tenKhac);
            var moTaSim = moTaChuan.Length > 0 && !string.IsNullOrWhiteSpace(x.MoTa)
                ? DoTuongDong(moTaChuan, ChuanHoa(x.MoTa))
                : 0d;
            var trungTen = tenSim >= NguongTrung;
            var trungMoTa = moTaSim >= NguongTrung && tenSim >= NguongTenKemMoTa;
            if (trungTen || trungMoTa)
                ketQua.Add(new DeTaiTrung(x.Id, x.Ten, Math.Round(Math.Max(tenSim, moTaSim), 2), trungTen));
        }
        return ketQua.OrderByDescending(x => x.DoTuongDong).ThenBy(x => x.Id).ToArray();
    }

    /// <summary>Chuẩn hóa: bỏ dấu tiếng Việt, đ → d, chữ thường, ký tự lạ thành khoảng trắng, gộp khoảng trắng.</summary>
    public static string ChuanHoa(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c is 'đ' or 'Đ') { sb.Append('d'); continue; }
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Hệ số Dice trên bigram của hai chuỗi đã chuẩn hóa, 0..1.</summary>
    public static double DoTuongDong(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1;
        if (a.Length < 2 || b.Length < 2) return 0;
        var ga = Bigram(a);
        var gb = Bigram(b);
        var giao = 0;
        foreach (var (k, v) in ga)
            if (gb.TryGetValue(k, out var w)) giao += Math.Min(v, w);
        return 2d * giao / (ga.Values.Sum() + gb.Values.Sum());
    }

    private static Dictionary<string, int> Bigram(string s)
    {
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < s.Length - 1; i++)
        {
            var k = s.Substring(i, 2);
            d[k] = d.GetValueOrDefault(k) + 1;
        }
        return d;
    }
}

public enum MucTaiGiangVien { Thap, Cao, DuQuota, VuotQuota }

/// <summary>Tải hiện tại của một GV trong đợt (SV đang có GVHD/GVĐD/đồng HD còn hiệu lực).</summary>
public sealed record TaiGiangVien(long GiangVienId, int DangCo, int Quota);

/// <summary>Đề tài trùng với một đề tài đợt khác (B10).</summary>
public sealed record DeTaiTrung(long Id, string TenDeTai, double DoTuongDong, bool TrungTen);
