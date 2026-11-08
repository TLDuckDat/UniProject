using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Seed;

/// <summary>
/// Dữ liệu tham chiếu đưa vào migration bằng <c>HasData</c> (có ở MỌI môi trường).
/// Id cố định – KHÔNG đổi sau khi đã có dữ liệu thật; thêm dòng mới thì dùng Id mới.
/// </summary>
internal static class ReferenceData
{
    public const long CoSoHaNoiId = 1;
    public const long CoSoHcmId = 2;
    public const long RuleSetUtc2021Id = 1;
    public const long RuleSetUtc2026Id = 2;

    /// <summary>NgayTao cố định cho dữ liệu HasData (migration phải ổn định, không dùng DateTime.UtcNow).</summary>
    public static readonly DateTime NgaySeed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly CoSo[] CoSo =
    [
        new() { NgayTao = NgaySeed, Id = CoSoHaNoiId, MaCoSo = "HN", TenCoSo = "Trường ĐH Giao thông Vận tải (UTC) – Hà Nội", ConHoatDong = true },
        new() { NgayTao = NgaySeed, Id = CoSoHcmId, MaCoSo = "HCM", TenCoSo = "Phân hiệu TP. Hồ Chí Minh (UTC2)", ConHoatDong = true },
    ];

    public static readonly VaiTro[] VaiTro =
    [
        new() { NgayTao = NgaySeed, Id = 1, MaVaiTro = "QuanTri", TenVaiTro = "Quản trị hệ thống", MoTa = "Quản lý tài khoản, phân quyền, cấu hình hệ thống." },
        new() { NgayTao = NgaySeed, Id = 2, MaVaiTro = "PhongDaoTao", TenVaiTro = "Phòng Đào tạo", MoTa = "Mở đợt, xét điều kiện, ghi nhận quyết định, chốt và sửa điểm." },
        new() { NgayTao = NgaySeed, Id = 3, MaVaiTro = "LanhDaoKhoa", TenVaiTro = "Lãnh đạo Khoa", MoTa = "Duyệt đề tài, đề xuất Hội đồng trong phạm vi khoa." },
        new() { NgayTao = NgaySeed, Id = 4, MaVaiTro = "TruongBoMon", TenVaiTro = "Trưởng Bộ môn", MoTa = "Phân công GVHD/GVĐD, duyệt ngoại lệ quota, tham số bộ môn." },
        new() { NgayTao = NgaySeed, Id = 5, MaVaiTro = "GiaoVu", TenVaiTro = "Giáo vụ", MoTa = "Xếp lịch bảo vệ, hỗ trợ nghiệp vụ khoa/bộ môn." },
        new() { NgayTao = NgaySeed, Id = 6, MaVaiTro = "GiangVien", TenVaiTro = "Giảng viên", MoTa = "Vai trò trên từng SV xác định qua PhanCong/ThanhVienHoiDong." },
        new() { NgayTao = NgaySeed, Id = 7, MaVaiTro = "SinhVien", TenVaiTro = "Sinh viên", MoTa = "Đăng ký đề tài, nộp tiến độ, nộp quyển, gửi đơn." },
        new() { NgayTao = NgaySeed, Id = 8, MaVaiTro = "ThuVien", TenVaiTro = "Thư viện", MoTa = "Tiếp nhận, lưu trữ bản cuối ĐATN." },
    ];

    /// <summary>Tham số theo docs/dac-ta-nghiep-vu.html mục 5.</summary>
    public static readonly RuleSet[] RuleSet =
    [
        new()
        {
            NgayTao = NgaySeed,
            Id = RuleSetUtc2021Id,
            MaRuleSet = "UTC-2021",
            TenRuleSet = "Quy chế đào tạo UTC 2021 (K64, K65 và các khóa cũ)",
            CanCuPhapLy = "QĐ 2006/QĐ-ĐHGTVT (nền Thông tư 08/2021/TT-BGDĐT)",
            MaxTCChuaTichLuy = 10,
            GpaBaoVeMin = 1.90m,
            GpaTotNghiepMin = 2.00m,
            DiemDatDATN = 5.5m,
            TrongSoQT = 0.30m, // ⚠O1: Khoa 30/70, Sổ tay K64 ghi 40/60 – chờ chốt.
            HeSoThoiGianToiDa = 2.0m,
            MinTVHoiDong = 3,
            MaxTVHoiDong = 5,
            NguongHaHangHocLaiPct = 5.0m, // học lại > 5% tổng TC
            HaHangKhiKyLuat = true,       // kỷ luật ≥ cảnh cáo
            HieuLucTu = new DateOnly(2021, 9, 1),
            HieuLucDen = null,
        },
        new()
        {
            NgayTao = NgaySeed,
            Id = RuleSetUtc2026Id,
            MaRuleSet = "UTC-2026",
            TenRuleSet = "Quy chế đào tạo UTC 2026 (K67 trở đi)",
            CanCuPhapLy = "Quy chế mới của trường theo Thông tư 56/2026",
            MaxTCChuaTichLuy = 10,
            GpaBaoVeMin = 1.90m,
            GpaTotNghiepMin = 2.00m,
            DiemDatDATN = 5.5m,
            TrongSoQT = 0.30m, // ⚠O1
            HeSoThoiGianToiDa = 2.0m,
            MinTVHoiDong = 3,
            MaxTVHoiDong = 5,
            NguongHaHangHocLaiPct = 10.0m, // ⚠O6: > 10% (tóm tắt TT 56, chưa đối chiếu văn bản gốc)
            HaHangKhiKyLuat = true,        // vi phạm liêm chính
            HieuLucTu = new DateOnly(2026, 9, 1),
            HieuLucDen = null,
        },
    ];

    public static readonly RuleSetKhoaHoc[] RuleSetKhoaHoc =
    [
        // K ≤ 65 (K64, K65 và các khóa cũ còn học).
        new() { NgayTao = NgaySeed, Id = 1, RuleSetId = RuleSetUtc2021Id, KhoaHocTu = 1, KhoaHocDen = 65, CanXacNhan = false },
        // ⚠O2: K66 chưa rõ quy chế – tạm UTC-2021, cần xác nhận.
        new() { NgayTao = NgaySeed, Id = 2, RuleSetId = RuleSetUtc2021Id, KhoaHocTu = 66, KhoaHocDen = 66, CanXacNhan = true },
        // K ≥ 67 (tuyển sinh 2026).
        new() { NgayTao = NgaySeed, Id = 3, RuleSetId = RuleSetUtc2026Id, KhoaHocTu = 67, KhoaHocDen = null, CanXacNhan = false },
    ];
}
