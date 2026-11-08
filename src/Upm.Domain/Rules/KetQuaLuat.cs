namespace Upm.Domain.Rules;

/// <summary>
/// Hình dạng chung của mọi kết quả luật (yêu cầu Phần 4 mục 8): UI hiển thị trực tiếp, và lưu vào snapshot
/// (<c>XetDieuKien</c>, <c>XetDieuKienChiTiet</c>, <c>DiemDATN</c>) để giải trình khiếu nại.
/// </summary>
public interface IKetQuaLuat
{
    /// <summary>Mã luật: G1–G3, K1–K6, DIEM_DATN… (xem <see cref="MaLuat"/>).</summary>
    string MaLuat { get; }

    /// <summary>Luật có được thỏa không.</summary>
    bool Dat { get; }

    /// <summary>Giải thích tiếng Việt có dấu, hiển thị cho người dùng.</summary>
    string LyDo { get; }

    /// <summary>Mã ngoại lệ trong đặc tả mục 10 (A01…G06) ứng với tình huống, nếu có.</summary>
    string? MaNgoaiLe { get; }
}

/// <summary>Kết quả của một luật đơn (một dòng G1, K4…).</summary>
public sealed record KetQuaLuat(string MaLuat, bool Dat, string LyDo, string? MaNgoaiLe = null) : IKetQuaLuat
{
    public static KetQuaLuat Qua(string maLuat, string lyDo, string? maNgoaiLe = null) =>
        new(maLuat, true, lyDo, maNgoaiLe);

    public static KetQuaLuat KhongQua(string maLuat, string lyDo, string? maNgoaiLe = null) =>
        new(maLuat, false, lyDo, maNgoaiLe);
}

/// <summary>Mã luật dùng trong <see cref="IKetQuaLuat.MaLuat"/>.</summary>
public static class MaLuat
{
    public const string RuleSetTheoKhoa = "RULESET_THEO_KHOA";
    public const string TCChuaTichLuy = "TC_CHUA_TICH_LUY";

    public const string XetGiao = "XET_GIAO";
    public const string G1 = "G1";
    public const string G2 = "G2";
    public const string G3 = "G3";

    public const string XetBaoVe = "XET_BAO_VE";
    public const string K1 = "K1";
    public const string K2 = "K2";
    public const string K3 = "K3";
    public const string K4 = "K4";
    public const string K5 = "K5";
    public const string K6 = "K6";
    public const string SuaGap = "K3_SUA_GAP";
    public const string HetHanHoan = "HET_HAN_HOAN_BAO_VE";

    public const string DiemDATN = "DIEM_DATN";
    public const string MoPhien = "MO_PHIEN";
    public const string ChotDiem = "CHOT_DIEM";
    public const string SoThanhVien = "SO_THANH_VIEN";
    public const string Quorum = "QUORUM";
    public const string DuPhieu = "DU_PHIEU";
    public const string TrucTuyen = "TRUC_TUYEN";
    public const string ChenhLech = "CHENH_LECH";

    public const string GpaTotNghiep = "GPA_TOT_NGHIEP";
    public const string HaHang = "HA_HANG";
}

/// <summary>Kết quả gồm nhiều luật con (mở phiên, chốt điểm…). <see cref="Dat"/> khi mọi luật con đạt.</summary>
public sealed record KetQuaTongHop(string MaLuat, IReadOnlyList<KetQuaLuat> CacLuat) : IKetQuaLuat
{
    public bool Dat => CacLuat.All(l => l.Dat);

    public string LyDo => Dat
        ? "Đạt tất cả điều kiện."
        : string.Join(" ", CacLuat.Where(l => !l.Dat).Select(l => l.LyDo));

    public string? MaNgoaiLe => CacLuat.FirstOrDefault(l => !l.Dat)?.MaNgoaiLe;
}
