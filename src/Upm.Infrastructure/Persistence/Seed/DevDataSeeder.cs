using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upm.Application.Abstractions;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;

namespace Upm.Infrastructure.Persistence.Seed;

/// <summary>
/// Dữ liệu mẫu CHỈ dùng cho Development (bật bằng <c>Database:SeedSampleData = true</c>).
/// Mỗi cơ sở: 1 khoa Kỹ thuật + 1 khoa Kinh tế, 2 bộ môn/khoa, 1 CTĐT/khoa (~15 học phần);
/// 6 giảng viên; 20 sinh viên với bảng điểm dựng theo kịch bản ngoại lệ A01–A09, G1–G3 (xem <see cref="Scenarios"/>).
/// Thêm tài khoản dev cho mỗi vai trò (mật khẩu <see cref="MatKhauDev"/>, ghi trong README).
/// Idempotent theo từng bước: đã có Khoa thì bỏ qua danh mục; đã có TaiKhoan thì bỏ qua tài khoản.
/// </summary>
public sealed class DevDataSeeder(UpmDbContext db, IClock clock, IPasswordHasher<TaiKhoan> hasher, ILogger<DevDataSeeder> logger)
{
    private const string NguonSeed = "SEED";

    /// <summary>
    /// Kỳ xét mẫu của đợt ĐATN dùng khi thử kịch bản (năm học 2026-2027, HK1). Đợt ở kỳ này cho đúng kết quả A01–A14
    /// với dữ liệu dưới đây (kỷ luật mẫu hiệu lực 01/09/2026–30/06/2027).
    /// </summary>
    internal static readonly HocKy KyXetMau = new(2026, 1);

    /// <summary>Mật khẩu chung của mọi tài khoản dev – CHỈ Development.</summary>
    public const string MatKhauDev = "Upm@123456";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedDanhMucAsync(ct);
        await SeedTaiKhoanAsync(ct);
    }

    /// <summary>
    /// Tài khoản dev: (tên đăng nhập, vai trò, phạm vi, liên kết GV/SV). Phạm vi viết dạng
    /// "HN" (cơ sở), "HN/CT" (khoa), "HN/CT/CAU" (bộ môn), null (toàn hệ thống).
    /// </summary>
    internal static readonly (string Ten, string VaiTro, string[] PhamVi, string? MaGV, string? MaSV)[] TaiKhoanDev =
    [
        ("admin", "QuanTri", [], null, null),
        ("pdt.hn", "PhongDaoTao", ["HN"], null, null),
        ("pdt.hcm", "PhongDaoTao", ["HCM"], null, null),
        ("pdt.2coso", "PhongDaoTao", ["HN", "HCM"], null, null),
        ("ldk.ct.hn", "LanhDaoKhoa", ["HN/CT"], null, null),
        ("tbm.cau.hn", "TruongBoMon", ["HN/CT/CAU"], "GV001", null),
        ("giaovu.ct.hn", "GiaoVu", ["HN/CT"], null, null),
        ("gv001", "GiangVien", ["HN/CT"], "GV001", null),
        ("gv003", "GiangVien", ["HN/KT"], "GV003", null),
        ("gv004", "GiangVien", ["HCM/CT"], "GV004", null),
        ("sv.65hn001", "SinhVien", ["HN"], null, "65HN001"),
        ("sv.65hn004", "SinhVien", ["HN"], null, "65HN004"),
        ("sv.65hn005", "SinhVien", ["HN"], null, "65HN005"),
        ("thuvien.hn", "ThuVien", ["HN"], null, null),
    ];

    private async Task SeedTaiKhoanAsync(CancellationToken ct)
    {
        if (await db.TaiKhoan.AnyAsync(ct))
        {
            return;
        }

        var vaiTro = await db.VaiTro.ToDictionaryAsync(v => v.MaVaiTro, v => v.Id, ct);
        var coSo = await db.CoSo.ToDictionaryAsync(c => c.MaCoSo, c => c.Id, ct);
        var khoa = await db.Khoa.ToListAsync(ct);
        var boMon = await db.BoMon.ToListAsync(ct);
        var gv = await db.GiangVien.ToDictionaryAsync(g => g.MaGV, g => g.Id, ct);
        var sv = await db.SinhVien.ToDictionaryAsync(s => s.MaSV, s => s.Id, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var dong = new List<(TaiKhoan Tk, string VaiTro, string[] PhamVi)>();
        foreach (var (ten, vt, phamVi, maGV, maSV) in TaiKhoanDev)
        {
            var tk = new TaiKhoan
            {
                TenDangNhap = ten,
                Email = $"{ten}@utc.edu.vn",
                GiangVienId = maGV is null ? null : gv[maGV],
                SinhVienId = maSV is null ? null : sv[maSV],
                LoaiTK = maSV is not null ? LoaiTaiKhoan.SV : maGV is not null ? LoaiTaiKhoan.GV : LoaiTaiKhoan.NoiBo,
                ConHoatDong = true,
            };
            tk.MatKhauHash = hasher.HashPassword(tk, MatKhauDev);
            db.TaiKhoan.Add(tk);
            dong.Add((tk, vt, phamVi));
        }
        await db.SaveChangesAsync(ct);

        foreach (var (tk, vt, phamVi) in dong)
        {
            foreach (var pv in phamVi.Length == 0 ? [string.Empty] : phamVi)
            {
                var phan = pv.Split('/', StringSplitOptions.RemoveEmptyEntries);
                long? coSoId = phan.Length >= 1 ? coSo[phan[0]] : null;
                long? khoaId = phan.Length >= 2 ? khoa.Single(k => k.CoSoId == coSoId && k.MaKhoa == phan[1]).Id : null;
                long? boMonId = phan.Length >= 3 ? boMon.Single(b => b.KhoaId == khoaId && b.MaBM == phan[2]).Id : null;
                db.TaiKhoanVaiTro.Add(new TaiKhoanVaiTro
                {
                    TaiKhoanId = tk.Id,
                    VaiTroId = vaiTro[vt],
                    // Lưu mức cụ thể nhất (khoa/bộ môn không cần CoSoId – suy ra khi nạp quyền).
                    CoSoId = khoaId is null ? coSoId : null,
                    KhoaId = boMonId is null ? khoaId : null,
                    BoMonId = boMonId,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Đã seed {SoTaiKhoan} tài khoản dev (mật khẩu: xem README).", dong.Count);
    }

    private async Task SeedDanhMucAsync(CancellationToken ct)
    {
        if (await db.Khoa.AnyAsync(ct))
        {
            logger.LogInformation("Đã có dữ liệu mẫu – bỏ qua seed danh mục.");
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = clock.Now.UtcDateTime;

        // ---------- Học phần (danh mục chung) ----------
        var hocPhan = HocPhanCatalog.CreateAll().ToDictionary(h => h.MaHP);
        db.HocPhan.AddRange(hocPhan.Values);

        // ---------- Khoa, bộ môn, CTĐT theo cơ sở ----------
        var ctdtByKey = new Dictionary<string, ChuongTrinhDaoTao>();
        var boMonByKey = new Dictionary<string, BoMon>();
        var khoaList = new List<Khoa>();

        foreach (var (coSoId, suffix, tenCoSo) in new[]
                 {
                     (ReferenceData.CoSoHaNoiId, "HN", "Hà Nội"),
                     (ReferenceData.CoSoHcmId, "HCM", "TP.HCM"),
                 })
        {
            var khoaKT = new Khoa { CoSoId = coSoId, MaKhoa = "CT", TenKhoa = $"Khoa Công trình ({tenCoSo})", KhoiNganh = KhoiNganh.KyThuat };
            var khoaKTe = new Khoa { CoSoId = coSoId, MaKhoa = "KT", TenKhoa = $"Khoa Kinh tế vận tải ({tenCoSo})", KhoiNganh = KhoiNganh.KinhTe };
            khoaList.AddRange([khoaKT, khoaKTe]);
            db.Khoa.AddRange(khoaKT, khoaKTe);
            await db.SaveChangesAsync(ct);

            boMonByKey[$"{suffix}-CAU"] = new BoMon { KhoaId = khoaKT.Id, MaBM = "CAU", TenBM = "Bộ môn Cầu hầm" };
            boMonByKey[$"{suffix}-DUONG"] = new BoMon { KhoaId = khoaKT.Id, MaBM = "DUONG", TenBM = "Bộ môn Đường bộ" };
            boMonByKey[$"{suffix}-KTXD"] = new BoMon { KhoaId = khoaKTe.Id, MaBM = "KTXD", TenBM = "Bộ môn Kinh tế xây dựng" };
            boMonByKey[$"{suffix}-QTKD"] = new BoMon { KhoaId = khoaKTe.Id, MaBM = "QTKD", TenBM = "Bộ môn Quản trị kinh doanh" };

            ctdtByKey[$"{suffix}-KT"] = new ChuongTrinhDaoTao
            {
                KhoaId = khoaKT.Id,
                MaCTDT = $"7580205-{suffix}",
                TenCTDT = "Kỹ sư Kỹ thuật xây dựng công trình giao thông",
                Nganh = "Kỹ thuật xây dựng công trình giao thông",
                He = HeDaoTao.KySuChuyenSau,
                ChuongTrinh = LoaiChuongTrinh.DaiTra,
                TongTCYeuCau = 180,
                TCDATN = 12,
                TCTTTN = 6, // cụm TTTN + ĐATN = 18
                CoHocPhanThayThe = false,
            };
            ctdtByKey[$"{suffix}-KTE"] = new ChuongTrinhDaoTao
            {
                KhoaId = khoaKTe.Id,
                MaCTDT = $"7840104-{suffix}",
                TenCTDT = "Cử nhân Kinh tế vận tải",
                Nganh = "Kinh tế vận tải",
                He = HeDaoTao.CuNhan,
                // HN: chất lượng cao (bật quét đạo văn); HCM: đại trà.
                ChuongTrinh = suffix == "HN" ? LoaiChuongTrinh.CLC : LoaiChuongTrinh.DaiTra,
                TongTCYeuCau = 130,
                TCDATN = 8,
                TCTTTN = 5, // cụm TTTN + ĐATN = 13
                CoHocPhanThayThe = true,
            };
        }

        db.BoMon.AddRange(boMonByKey.Values);
        db.ChuongTrinhDaoTao.AddRange(ctdtByKey.Values);
        await db.SaveChangesAsync(ct);

        // ---------- CTĐT – học phần ----------
        foreach (var (key, ctdt) in ctdtByKey)
        {
            var plan = key.EndsWith("-KTE", StringComparison.Ordinal) ? HocPhanCatalog.KeHoachKinhTe : HocPhanCatalog.KeHoachKyThuat;
            db.CTDTHocPhan.AddRange(plan.Select(p => new CTDTHocPhan
            {
                CTDTId = ctdt.Id,
                HocPhanId = hocPhan[p.MaHP].Id,
                HocKyKeHoach = p.HocKy,
                LaHPThayTheDATN = p.ThayTheDATN,
            }));
        }

        // ---------- Tham số bộ môn (cho cả 2 RuleSet) & đạo văn ----------
        foreach (var bm in boMonByKey.Values)
        {
            foreach (var ruleSetId in new[] { ReferenceData.RuleSetUtc2021Id, ReferenceData.RuleSetUtc2026Id })
            {
                db.ThamSoBoMon.Add(new ThamSoBoMon
                {
                    BoMonId = bm.Id,
                    RuleSetId = ruleSetId,
                    TrongSoQT = 0.30m, // ⚠O1
                    TrongSoHD = 0.70m,
                    TachDiemKhac = false,
                    SoNgaySuaGapToiDa = 3,
                    DungHanKhiNghiTamThoi = true, // ⚠O4
                });
            }
        }

        foreach (var khoa in khoaList)
        {
            if (khoa.KhoiNganh == KhoiNganh.KyThuat)
            {
                // Kỹ thuật đại trà: tắt quét đạo văn.
                db.ThamSoDaoVan.Add(new ThamSoDaoVan { KhoaId = khoa.Id, ChuongTrinh = LoaiChuongTrinh.DaiTra, Bat = false, NguongPhanTram = 25.0m });
            }
            else
            {
                db.ThamSoDaoVan.Add(new ThamSoDaoVan { KhoaId = khoa.Id, ChuongTrinh = LoaiChuongTrinh.CLC, Bat = true, NguongPhanTram = 20.0m });
                db.ThamSoDaoVan.Add(new ThamSoDaoVan { KhoaId = khoa.Id, ChuongTrinh = LoaiChuongTrinh.DaiTra, Bat = true, NguongPhanTram = 25.0m });
            }
        }

        // ---------- Giảng viên ----------
        db.GiangVien.AddRange(
            Gv("GV001", "PGS.TS Trần Minh Cường", "PGS", "TS", LoaiGiangVien.CoHuu, 10, boMonByKey["HN-CAU"], ReferenceData.CoSoHaNoiId),
            Gv("GV002", "TS Lê Thị Hoa", null, "TS", LoaiGiangVien.CoHuu, 8, boMonByKey["HN-DUONG"], ReferenceData.CoSoHaNoiId),
            Gv("GV003", "ThS Phạm Quốc Bảo", null, "ThS", LoaiGiangVien.CoHuu, 5, boMonByKey["HN-KTXD"], ReferenceData.CoSoHaNoiId),
            Gv("GV004", "TS Nguyễn Thanh Tùng", null, "TS", LoaiGiangVien.CoHuu, 8, boMonByKey["HCM-CAU"], ReferenceData.CoSoHcmId),
            Gv("GV005", "ThS Võ Ngọc Lan", null, "ThS", LoaiGiangVien.CoHuu, 6, boMonByKey["HCM-QTKD"], ReferenceData.CoSoHcmId),
            Gv("GV006", "TS Đỗ Hải Nam (ngoài trường)", null, "TS", LoaiGiangVien.NgoaiTruong, 5, boMonByKey["HN-DUONG"], ReferenceData.CoSoHaNoiId));

        // ---------- Sinh viên + tiến độ + bảng điểm + kỷ luật ----------
        for (var stt = 0; stt < Scenarios.Count; stt++)
        {
            var s = Scenarios[stt];
            var coSoId = s.CoSo == "HN" ? ReferenceData.CoSoHaNoiId : ReferenceData.CoSoHcmId;
            var ctdt = ctdtByKey[$"{s.CoSo}-{s.Khoi}"];
            var laKyThuat = s.Khoi == "KT";
            var nhapHoc = new DateOnly(1959 + s.KhoaHoc, 9, 1); // K67 = tuyển sinh 2026
            var soNamChuan = laKyThuat ? 5 : 4;

            var sv = new SinhVien
            {
                MaSV = s.MaSV,
                HoTen = s.HoTen,
                NgaySinh = new DateOnly(1941 + s.KhoaHoc, 1 + stt % 12, 15),
                KhoaHoc = s.KhoaHoc,
                CTDTId = ctdt.Id,
                CoSoId = coSoId,
                TrangThaiHocTap = s.TrangThaiHocTap,
                NgayNhapHoc = nhapHoc,
                Email = $"{s.MaSV.ToLowerInvariant()}@st.utc.edu.vn",
            };
            db.SinhVien.Add(sv);
            await db.SaveChangesAsync(ct);

            var plan = laKyThuat ? HocPhanCatalog.KeHoachKyThuat : HocPhanCatalog.KeHoachKinhTe;
            var kyCuoi = plan.Max(p => p.HocKy);
            var kyCuoiKeHoach = TenHocKy(nhapHoc.Year, kyCuoi);
            // Rule engine xác định "đúng tiến độ" = kỳ xét ≤ KyCuoiKeHoach (Phần 4). SV kịch bản trễ tiến độ (A07) phải có
            // kỳ cuối kế hoạch TRƯỚC kỳ xét mẫu, nếu không thì cờ DungTienDo = false và kết quả xét mâu thuẫn nhau.
            if (!s.DungTienDo && HocKy.Parse(kyCuoiKeHoach) >= KyXetMau)
            {
                kyCuoiKeHoach = new HocKy(KyXetMau.NamHoc - 1, 2).ToString(); // HK2 năm học trước kỳ xét mẫu
            }
            db.ThongTinTienDoSV.Add(new ThongTinTienDoSV
            {
                SinhVienId = sv.Id,
                KyCuoiKeHoach = kyCuoiKeHoach,
                HanToiDa = nhapHoc.AddYears(soNamChuan * 2), // 2 × thời gian chuẩn
                DungTienDo = s.DungTienDo,
                DongBoLuc = now,
            });

            var i = 0;
            foreach (var p in plan.Where(p => !p.ThayTheDATN && hocPhan[p.MaHP].LoaiHP != LoaiHocPhan.DATN))
            {
                var hp = hocPhan[p.MaHP];
                var hocKy = TenHocKy(nhapHoc.Year, p.HocKy);
                var kieu = s.Diem.GetValueOrDefault(p.MaHP, KieuDiem.Dat);
                if (kieu == KieuDiem.ChuaHoc)
                {
                    continue;
                }

                var diemDat = Math.Clamp(s.DiemNen + Offsets[i++ % Offsets.Length], 4.0m, 10.0m);
                switch (kieu)
                {
                    case KieuDiem.Dat:
                        db.BangDiem.Add(Diem(sv.Id, hp.Id, hocKy, 1, diemDat, TrangThaiDiem.Dat, now));
                        break;
                    case KieuDiem.Rot:
                        db.BangDiem.Add(Diem(sv.Id, hp.Id, hocKy, 1, 3.0m, TrangThaiDiem.Rot, now));
                        break;
                    case KieuDiem.HocLai:
                        db.BangDiem.Add(Diem(sv.Id, hp.Id, hocKy, 1, 3.5m, TrangThaiDiem.Rot, now));
                        db.BangDiem.Add(Diem(sv.Id, hp.Id, TenHocKy(nhapHoc.Year, p.HocKy + 2), 2, diemDat, TrangThaiDiem.Dat, now));
                        break;
                    case KieuDiem.DangHoc:
                        db.BangDiem.Add(KhongDiem(sv.Id, hp.Id, TenHocKy(nhapHoc.Year, kyCuoi), TrangThaiDiem.DangHoc, null, now));
                        break;
                    case KieuDiem.I:
                        db.BangDiem.Add(KhongDiem(sv.Id, hp.Id, hocKy, TrangThaiDiem.I, "I", now));
                        break;
                    case KieuDiem.X:
                        db.BangDiem.Add(KhongDiem(sv.Id, hp.Id, hocKy, TrangThaiDiem.X, "X", now));
                        break;
                    case KieuDiem.R:
                        db.BangDiem.Add(KhongDiem(sv.Id, hp.Id, hocKy, TrangThaiDiem.R, "R", now));
                        break;
                }
            }

            if (s.KyLuat is { } kl)
            {
                db.KyLuatSV.Add(new KyLuatSV
                {
                    SinhVienId = sv.Id,
                    MucKyLuat = kl.Muc,
                    TuNgay = new DateOnly(2026, 9, 1),
                    DenNgay = kl.DinhChi ? new DateOnly(2027, 6, 30) : null,
                    DinhChiHocTap = kl.DinhChi,
                    TruyCuuHinhSu = kl.HinhSu,
                    NguonCTSV = NguonSeed,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        int soKhoa = khoaList.Count, soSV = Scenarios.Count;
        logger.LogInformation("Đã seed dữ liệu mẫu: {SoKhoa} khoa, {SoSV} sinh viên.", soKhoa, soSV);
    }

    // =====================================================================
    // Kịch bản sinh viên – mỗi SV minh họa một ngoại lệ (mã theo dac-ta-nghiep-vu.html mục 10).
    // TC học phần xem HocPhanCatalog. KT = Kỹ thuật (TTTN 6 TC), KTE = Kinh tế (TTTN 5 TC, có HP thay thế).
    // =====================================================================
    internal static readonly IReadOnlyList<KichBanSV> Scenarios =
    [
        new("65HN001", "Nguyễn Văn An", 65, "HN", "KT", 7.5m, "Đủ điều kiện, đã qua TTTN, không nợ"),
        new("65HN002", "Trần Thị Bình", 65, "HN", "KT", 7.0m, "A01: nợ 6 TC (KTH104 4 + KTH108 2) ≤ 10 → giao + cờ NO_TIN_CHI",
            Diem: new() { ["KTH104"] = KieuDiem.Rot, ["KTH108"] = KieuDiem.Rot }),
        new("65HN003", "Lê Văn Cường", 65, "HN", "KT", 6.5m, "A02: nợ 14 TC > 10 → KHONG_DU_DK",
            Diem: new() { ["KTH104"] = KieuDiem.Rot, ["KTH106"] = KieuDiem.Rot, ["KTH107"] = KieuDiem.Rot, ["KTH109"] = KieuDiem.Rot }),
        new("65HN004", "Phạm Thu Dung", 65, "HN", "KT", 7.0m, "A03: nợ ĐÚNG 10 TC (4 + 3 + 3) → vẫn được giao",
            Diem: new() { ["KTH104"] = KieuDiem.Rot, ["KTH105"] = KieuDiem.Rot, ["KTH107"] = KieuDiem.Rot }),
        new("65HN005", "Hoàng Văn Em", 65, "HN", "KT", 7.2m, "A04: 2 môn đang học chưa có điểm (3 + 2) + nợ 2 TC = 7",
            Diem: new() { ["KTH109"] = KieuDiem.DangHoc, ["KTH110"] = KieuDiem.DangHoc, ["KTH108"] = KieuDiem.Rot }),
        new("65HN006", "Vũ Thị Giang", 65, "HN", "KT", 7.0m, "A05: nợ GDQP + GDTC (không tính) + nợ KTH103 3 TC → 3 TC",
            Diem: new() { ["GDQP"] = KieuDiem.Rot, ["GDTC"] = KieuDiem.Rot, ["KTH103"] = KieuDiem.Rot }),
        new("65HN007", "Đặng Văn Hải", 65, "HN", "KT", 7.8m, "A06: đúng tiến độ, chưa học TTTN → TTTN không tính, cờ CHUA_QUA_TTTN",
            Diem: new() { ["KTH-TTTN"] = KieuDiem.ChuaHoc }),
        new("65HN008", "Bùi Thị Khánh", 65, "HN", "KT", 6.8m, "A07: TRỄ tiến độ, chưa có TTTN: nợ 5 TC + TTTN 6 TC = 11 > 10 → KHONG_DU_DK",
            DungTienDo: false, Diem: new() { ["KTH-TTTN"] = KieuDiem.ChuaHoc, ["KTH105"] = KieuDiem.Rot, ["KTH108"] = KieuDiem.Rot }),
        new("65HN009", "Ngô Văn Long", 65, "HN", "KT", 7.1m, "A08: điểm I (KTH106 4 TC) + điểm X (KTH107 3 TC) = 7 TC chưa tích lũy",
            Diem: new() { ["KTH106"] = KieuDiem.I, ["KTH107"] = KieuDiem.X }),
        new("65HN010", "Đỗ Thị Mai", 65, "HN", "KT", 8.0m, "A09: KTH101, KTH102 điểm R (miễn) → đã tích lũy, không vào GPA",
            Diem: new() { ["KTH101"] = KieuDiem.R, ["KTH102"] = KieuDiem.R }),
        new("65HN011", "Lý Văn Nam", 65, "HN", "KTE", 6.5m, "Kinh tế có HP thay thế, nợ 12 TC > 10 → KHONG_DU_DK_CHUYEN_THAY_THE (T02)",
            Diem: new() { ["KTE104"] = KieuDiem.Rot, ["KTE107"] = KieuDiem.Rot, ["KTE108"] = KieuDiem.Rot, ["KTE109"] = KieuDiem.Rot }),
        new("65HN012", "Trịnh Thị Oanh", 65, "HN", "KT", 7.0m, "G1: đang bị đình chỉ học tập → KHONG_DU_DK",
            KyLuat: new("Đình chỉ học tập có thời hạn", DinhChi: true, HinhSu: false)),
        new("65HCM013", "Mai Văn Phúc", 65, "HCM", "KT", 7.0m, "G1: bị truy cứu trách nhiệm hình sự → KHONG_DU_DK",
            KyLuat: new("Bị truy cứu trách nhiệm hình sự", DinhChi: false, HinhSu: true)),
        new("65HCM014", "Châu Thị Quyên", 65, "HCM", "KTE", 7.0m, "G2: đang nghỉ học tạm thời → KHONG_DU_DK",
            TrangThaiHocTap: TrangThaiHocTap.NghiTamThoi),
        new("55HCM015", "Tạ Văn Sơn", 55, "HCM", "KTE", 6.0m, "G2: K55, quá thời gian học tối đa (2 × 4 năm) → KHONG_DU_DK",
            DungTienDo: false),
        new("65HCM016", "Hồ Thị Trang", 65, "HCM", "KT", 5.0m, "GPA thấp (~1,6–1,8): đủ giao nhưng sẽ trượt K5 (GPA < 1,90) khi xét bảo vệ"),
        new("64HCM017", "Kiều Văn Uy", 64, "HCM", "KTE", 7.5m, "Học lại KTE103, KTE110 (LanHoc 2 đạt) – dữ liệu cho hạ hạng tốt nghiệp",
            Diem: new() { ["KTE103"] = KieuDiem.HocLai, ["KTE110"] = KieuDiem.HocLai }),
        new("66HN018", "Phan Thị Vân", 66, "HN", "KT", 7.3m, "O2: K66 → UTC-2021 (CanXacNhan = 1)"),
        new("67HCM019", "Lưu Văn Xuân", 67, "HCM", "KTE", 7.6m, "K67 → RuleSet UTC-2026"),
        new("64HN020", "Dương Thị Yến", 64, "HN", "KTE", 7.0m, "Đúng tiến độ: nợ 5 TC (KTE104 3 + KTE105 2) + chưa học TTTN → giao, cờ NO_TIN_CHI + CHUA_QUA_TTTN",
            Diem: new() { ["KTE-TTTN"] = KieuDiem.ChuaHoc, ["KTE104"] = KieuDiem.Rot, ["KTE105"] = KieuDiem.Rot }),
    ];

    private static readonly decimal[] Offsets = [0m, 0.5m, -0.5m, 1.0m, -1.0m];

    private static GiangVien Gv(string ma, string ten, string? hocHam, string? hocVi, LoaiGiangVien loai, int quota, BoMon bm, long coSoId) => new()
    {
        MaGV = ma,
        HoTen = ten,
        HocHam = hocHam,
        HocVi = hocVi,
        LoaiGV = loai,
        QuotaToiDa = quota,
        BoMonId = bm.Id,
        CoSoId = coSoId,
        Email = $"{ma.ToLowerInvariant()}@utc.edu.vn",
        ConHoatDong = true,
    };

    /// <summary>Học kỳ dạng "2024.1" (năm bắt đầu năm học . kỳ 1|2).</summary>
    internal static string TenHocKy(int namNhapHoc, int hocKyKeHoach) =>
        $"{namNhapHoc + (hocKyKeHoach - 1) / 2}.{(hocKyKeHoach - 1) % 2 + 1}";

    private static BangDiem Diem(long svId, long hpId, string hocKy, int lan, decimal diem10, TrangThaiDiem tt, DateTime now)
    {
        var (chu, he4) = ThangDiem.Quy(diem10);
        return new BangDiem
        {
            SinhVienId = svId,
            HocPhanId = hpId,
            HocKy = hocKy,
            LanHoc = lan,
            Diem10 = diem10,
            DiemChu = chu,
            Diem4 = he4,
            TrangThaiDiem = tt,
            NguonDongBo = NguonSeed,
            DongBoLuc = now,
        };
    }

    private static BangDiem KhongDiem(long svId, long hpId, string hocKy, TrangThaiDiem tt, string? diemChu, DateTime now) => new()
    {
        SinhVienId = svId,
        HocPhanId = hpId,
        HocKy = hocKy,
        LanHoc = 1,
        Diem10 = null,
        DiemChu = diemChu,
        Diem4 = null,
        TrangThaiDiem = tt,
        NguonDongBo = NguonSeed,
        DongBoLuc = now,
    };
}

internal enum KieuDiem { Dat, Rot, HocLai, DangHoc, I, X, R, ChuaHoc }

internal sealed record KyLuatMau(string Muc, bool DinhChi, bool HinhSu);

internal sealed record KichBanSV(
    string MaSV,
    string HoTen,
    int KhoaHoc,
    string CoSo,
    string Khoi,
    decimal DiemNen,
    string MoTa,
    bool DungTienDo = true,
    TrangThaiHocTap TrangThaiHocTap = TrangThaiHocTap.DangHoc,
    Dictionary<string, KieuDiem>? Diem = null,
    KyLuatMau? KyLuat = null)
{
    public Dictionary<string, KieuDiem> Diem { get; init; } = Diem ?? [];
}

/// <summary>Quy đổi thang 10 → chữ / thang 4 (Sổ tay UTC2, dac-ta mục 8). Chỉ dùng để seed dữ liệu mẫu.</summary>
internal static class ThangDiem
{
    private static readonly (decimal Tu, string Chu, decimal He4)[] Bang =
    [
        (9.5m, "A+", 4.0m), (8.5m, "A", 3.8m), (8.0m, "B+", 3.5m), (7.0m, "B", 3.0m), (6.0m, "C+", 2.5m),
        (5.5m, "C", 2.0m), (4.5m, "D+", 1.5m), (4.0m, "D", 1.0m), (2.0m, "F+", 0.5m), (0m, "F", 0m),
    ];

    public static (string Chu, decimal He4) Quy(decimal diem10)
    {
        var dong = Bang.First(b => diem10 >= b.Tu);
        return (dong.Chu, dong.He4);
    }
}

/// <summary>Danh mục học phần mẫu và kế hoạch học tập của 2 CTĐT mẫu.</summary>
internal static class HocPhanCatalog
{
    /// <summary>Tạo instance MỚI mỗi lần (entity đã lưu sẽ mang Id – không dùng lại giữa các DbContext).</summary>
    public static HocPhan[] CreateAll() =>
    [
        Hp("GDQP", "Giáo dục quốc phòng – an ninh", 8, LoaiHocPhan.GDQP, tinhDiem: false),
        Hp("GDTC", "Giáo dục thể chất", 4, LoaiHocPhan.GDTC, tinhDiem: false),
        // Kỹ thuật
        Hp("KTH101", "Toán cao cấp", 3),
        Hp("KTH102", "Vật lý đại cương", 3),
        Hp("KTH103", "Cơ học kỹ thuật", 3),
        Hp("KTH104", "Sức bền vật liệu", 4),
        Hp("KTH105", "Kết cấu thép", 3),
        Hp("KTH106", "Kết cấu bê tông cốt thép", 4),
        Hp("KTH107", "Thiết kế cầu", 3),
        Hp("KTH108", "Thiết kế đường ô tô", 2),
        Hp("KTH109", "Thi công công trình giao thông", 3),
        Hp("KTH110", "Đồ án môn học thiết kế cầu", 2),
        Hp("KTH111", "Tin học ứng dụng trong xây dựng", 3),
        Hp("KTH-TTTN", "Thực tập tốt nghiệp (Kỹ thuật)", 6, LoaiHocPhan.TTTN),
        Hp("KTH-DATN", "Đồ án tốt nghiệp (Kỹ thuật)", 12, LoaiHocPhan.DATN),
        // Kinh tế
        Hp("KTE101", "Kinh tế vi mô", 3),
        Hp("KTE102", "Kinh tế vĩ mô", 3),
        Hp("KTE103", "Nguyên lý kế toán", 3),
        Hp("KTE104", "Tài chính doanh nghiệp", 3),
        Hp("KTE105", "Marketing căn bản", 2),
        Hp("KTE106", "Quản trị học", 2),
        Hp("KTE107", "Thống kê kinh doanh", 3),
        Hp("KTE108", "Kinh tế vận tải", 3),
        Hp("KTE109", "Logistics và chuỗi cung ứng", 3),
        Hp("KTE110", "Phân tích hoạt động kinh doanh", 4),
        Hp("KTE-TTTN", "Thực tập tốt nghiệp (Kinh tế)", 5, LoaiHocPhan.TTTN),
        Hp("KTE-DATN", "Khóa luận tốt nghiệp (Kinh tế)", 8, LoaiHocPhan.DATN),
        Hp("KTE-TT01", "Chuyên đề thay thế 1: Quản trị vận tải đa phương thức", 4),
        Hp("KTE-TT02", "Chuyên đề thay thế 2: Kinh tế cảng biển", 4),
    ];

    /// <summary>15 học phần, 10 học kỳ (kỹ sư 5 năm).</summary>
    public static readonly KeHoach[] KeHoachKyThuat =
    [
        new("GDQP", 1), new("GDTC", 2), new("KTH101", 1), new("KTH102", 2), new("KTH103", 3),
        new("KTH104", 4), new("KTH105", 5), new("KTH106", 6), new("KTH107", 7), new("KTH108", 7),
        new("KTH109", 8), new("KTH110", 8), new("KTH111", 3), new("KTH-TTTN", 9), new("KTH-DATN", 10),
    ];

    /// <summary>16 học phần, 8 học kỳ (cử nhân 4 năm), có 2 HP thay thế ĐATN.</summary>
    public static readonly KeHoach[] KeHoachKinhTe =
    [
        new("GDQP", 1), new("GDTC", 2), new("KTE101", 1), new("KTE102", 2), new("KTE103", 2),
        new("KTE104", 3), new("KTE105", 3), new("KTE106", 4), new("KTE107", 4), new("KTE108", 5),
        new("KTE109", 6), new("KTE110", 6), new("KTE-TTTN", 7), new("KTE-DATN", 8),
        new("KTE-TT01", 8, ThayTheDATN: true), new("KTE-TT02", 8, ThayTheDATN: true),
    ];

    private static HocPhan Hp(string ma, string ten, int tc, LoaiHocPhan loai = LoaiHocPhan.Thuong, bool tinhDiem = true) =>
        new() { MaHP = ma, TenHP = ten, SoTC = tc, LoaiHP = loai, TinhDiem = tinhDiem };
}

internal sealed record KeHoach(string MaHP, int HocKy, bool ThayTheDATN = false);
