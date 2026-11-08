using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Domain.Tests.Rules;

/// <summary>RuleSet mẫu – cùng giá trị seed (ReferenceData), trừ trọng số được ghi rõ trong test.</summary>
internal static class RuleSetMau
{
    public static RuleSet Entity2021(decimal trongSoQT = 0.30m) => new()
    {
        MaRuleSet = "UTC-2021",
        MaxTCChuaTichLuy = 10,
        GpaBaoVeMin = 1.90m,
        GpaTotNghiepMin = 2.00m,
        DiemDatDATN = 5.5m,
        TrongSoQT = trongSoQT,
        HeSoThoiGianToiDa = 2.0m,
        MinTVHoiDong = 3,
        MaxTVHoiDong = 5,
        NguongHaHangHocLaiPct = 5.0m,
        HaHangKhiKyLuat = true,
    };

    public static RuleSet Entity2026(decimal trongSoQT = 0.30m)
    {
        var rs = Entity2021(trongSoQT);
        rs.MaRuleSet = "UTC-2026";
        rs.NguongHaHangHocLaiPct = 10.0m;
        return rs;
    }

    public static ThamSoQuyChe Utc2021 { get; } = ThamSoQuyChe.Tu(Entity2021());

    public static ThamSoQuyChe Utc2026 { get; } = ThamSoQuyChe.Tu(Entity2026(), haHangKhiViPhamLiemChinh: true);
}

/// <summary>Lịch học kỳ giả: kỳ 1 kết thúc 15/01 năm sau, kỳ 2 kết thúc 30/06 năm sau, kỳ 3 kết thúc 31/08 năm sau.</summary>
internal sealed class LichHocKyGia : ILichHocKy
{
    public static LichHocKyGia Instance { get; } = new();

    public DateOnly NgayKetThuc(HocKy hocKy) => hocKy.Ky switch
    {
        1 => new DateOnly(hocKy.NamHoc + 1, 1, 15),
        2 => new DateOnly(hocKy.NamHoc + 1, 6, 30),
        _ => new DateOnly(hocKy.NamHoc + 1, 8, 31),
    };
}

/// <summary>Dựng CTĐT + bảng điểm cho một SV: mỗi lời gọi thêm một học phần mới vào CTĐT.</summary>
internal sealed class HocTap
{
    public static readonly HocKy KyXet = HocKy.Parse("2026.1");

    private readonly List<HocPhanCTDT> _ctdt = [];
    private readonly List<DiemHocPhan> _diem = [];
    private HocKy _kyCuoi = KyXet;
    private long _id;

    /// <summary>SV đúng tiến độ: kỳ cuối kế hoạch = kỳ xét.</summary>
    public static HocTap DungTienDo() => new();

    /// <summary>SV trễ tiến độ: kỳ cuối kế hoạch đã qua.</summary>
    public static HocTap TreTienDo() => new() { _kyCuoi = HocKy.Parse("2025.1") };

    /// <summary>Phần đã tích lũy của CTĐT (để có dữ liệu như thật).</summary>
    public HocTap NenDaDat(int soHP = 5) => Lap(soHP, () => Dat(3));

    public HocTap Dat(int tc, LoaiHocPhan loai = LoaiHocPhan.Thuong) => Them(tc, loai, TrangThaiDiem.Dat);

    public HocTap No(int tc, LoaiHocPhan loai = LoaiHocPhan.Thuong) => Them(tc, loai, TrangThaiDiem.Rot);

    public HocTap DangHoc(int tc, LoaiHocPhan loai = LoaiHocPhan.Thuong) => Them(tc, loai, TrangThaiDiem.DangHoc);

    public HocTap DiemI(int tc) => Them(tc, LoaiHocPhan.Thuong, TrangThaiDiem.I);

    public HocTap DiemX(int tc) => Them(tc, LoaiHocPhan.Thuong, TrangThaiDiem.X);

    public HocTap DiemR(int tc) => Them(tc, LoaiHocPhan.Thuong, TrangThaiDiem.R);

    /// <summary>Học phần có trong CTĐT nhưng chưa có dòng điểm nào.</summary>
    public HocTap ChuaHoc(int tc, LoaiHocPhan loai = LoaiHocPhan.Thuong) => Them(tc, loai, null);

    /// <summary>Rớt lần 1, lần 2 có trạng thái <paramref name="lan2"/>.</summary>
    public HocTap HocLai(int tc, TrangThaiDiem lan2)
    {
        Them(tc, LoaiHocPhan.Thuong, TrangThaiDiem.Rot);
        _diem.Add(new DiemHocPhan(_id, lan2, 2));
        return this;
    }

    public HocTap KhongTinhDiem(int tc)
    {
        _ctdt.Add(new HocPhanCTDT(++_id, $"HP{_id:00}", tc, LoaiHocPhan.Thuong, TinhDiem: false));
        return this;
    }

    public HocTap ThayTheDATN(int tc)
    {
        _ctdt.Add(new HocPhanCTDT(++_id, $"HP{_id:00}", tc, LoaiHocPhan.Thuong, LaHPThayTheDATN: true));
        return this;
    }

    public DuLieuHocTapSV Build() => new() { HocPhanCTDT = _ctdt.ToList(), BangDiem = _diem.ToList(), KyCuoiKeHoach = _kyCuoi };

    private HocTap Lap(int n, Action a)
    {
        for (var i = 0; i < n; i++)
        {
            a();
        }

        return this;
    }

    private HocTap Them(int tc, LoaiHocPhan loai, TrangThaiDiem? trangThai)
    {
        _ctdt.Add(new HocPhanCTDT(++_id, $"HP{_id:00}", tc, loai));
        if (trangThai is { } t)
        {
            _diem.Add(new DiemHocPhan(_id, t));
        }

        return this;
    }
}

internal static class HoSo
{
    public static readonly DateOnly NgayXet = new(2026, 9, 7);

    public static HoSoXetGiao Giao(HocTap hocTap, bool coHPThayThe = false, IReadOnlyList<KyLuat>? kyLuat = null,
        TrangThaiHocTap trangThai = TrangThaiHocTap.DangHoc, DateOnly? hanToiDa = null) => new()
    {
        TrangThaiHocTap = trangThai,
        KyLuat = kyLuat ?? [],
        HanToiDa = hanToiDa ?? new DateOnly(2029, 8, 31),
        HocTap = hocTap.Build(),
        CTDTCoHocPhanThayThe = coHPThayThe,
    };

    public static HoSoXetBaoVe BaoVe(HocTap? hocTap = null, decimal gpa = 2.50m, bool gvhd = true,
        KetQuaDocDuyetLoai gvdd = KetQuaDocDuyetLoai.DongY, IReadOnlyList<KyLuat>? kyLuat = null) => new()
    {
        KyLuat = kyLuat ?? [],
        GVHDDongY = gvhd,
        LyDoGVHD = gvhd ? null : "Không đạt yêu cầu tiến độ.",
        KetQuaGVDD = gvdd,
        HocTap = (hocTap ?? HocTap.DungTienDo().NenDaDat().Dat(5, LoaiHocPhan.TTTN).ChuaHoc(10, LoaiHocPhan.DATN)).Build(),
        GpaTichLuyTho = gpa,
    };

    public static KyLuat DinhChi(DateOnly tu, DateOnly? den = null) => new(MucKyLuat.DinhChiCoThoiHan, tu, den, true, false);

    public static KyLuat HinhSu(DateOnly tu) => new(MucKyLuat.KhienTrach, tu, null, false, true);
}
