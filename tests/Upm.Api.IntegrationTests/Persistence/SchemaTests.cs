using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Domain.Entities;
using Upm.Infrastructure.Persistence;

namespace Upm.Api.IntegrationTests.Persistence;

/// <summary>
/// Kiểm tra schema sinh từ migration trên SQL Server sạch (Testcontainers) khớp docs/thiet-ke.md.
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class SchemaTests(UpmApiFactory factory)
{
    private static readonly string[] ExpectedTables =
    [
        // ERD-01
        "CoSo", "Khoa", "BoMon", "GiangVien", "SinhVien", "ChuongTrinhDaoTao", "TaiKhoan", "VaiTro", "TaiKhoanVaiTro",
        // ERD-02
        "RuleSet", "RuleSetKhoaHoc", "ThamSoBoMon", "ThamSoDaoVan", "HocPhan", "CTDTHocPhan", "ThongTinTienDoSV", "BangDiem", "KyLuatSV",
        // ERD-03
        "DotDATN", "SVTrongDot", "XetDieuKien", "XetDieuKienChiTiet",
        // ERD-04
        "MocTienDo", "DeTai", "BaoCaoTienDo", "DangKyNguyenVong", "PhanCong", "PhieuGiaoNhiemVu",
        // ERD-05
        "PhienBanQuyen", "YKienGVHD", "KetQuaDocDuyet", "KetQuaKiemTraDaoVan", "LuuTruThuVien",
        // ERD-06
        "HoiDong", "ThanhVienHoiDong", "PhongBaoVe", "LichBaoVe", "PhieuDiem", "BienBan", "DiemDATN",
        // ERD-07
        "QuyetDinh", "QuyetDinhDoiTuong", "DonTu", "LichSuTrangThai", "AuditLog", "LichNhacViec",
        // Bổ sung Phần 3 (xác thực – không có trong thiet-ke.md)
        "RefreshToken",
    ];

    /// <summary>MỌI cột decimal trong thiet-ke.md với (precision, scale) mong đợi.</summary>
    private static readonly (string Table, string Column, int Precision, int Scale)[] ExpectedDecimals =
    [
        ("RuleSet", "GpaBaoVeMin", 3, 2), ("RuleSet", "GpaTotNghiepMin", 3, 2), ("RuleSet", "DiemDatDATN", 3, 1),
        ("RuleSet", "TrongSoQT", 3, 2), ("RuleSet", "HeSoThoiGianToiDa", 3, 1), ("RuleSet", "NguongHaHangHocLaiPct", 4, 1),
        ("ThamSoBoMon", "TrongSoQT", 3, 2), ("ThamSoBoMon", "TrongSoHD", 3, 2),
        ("ThamSoDaoVan", "NguongPhanTram", 4, 1),
        ("BangDiem", "Diem10", 3, 1), ("BangDiem", "Diem4", 2, 1),
        ("XetDieuKien", "GpaTichLuy", 3, 2),
        ("MocTienDo", "YeuCauToiThieuPct", 5, 2),
        ("BaoCaoTienDo", "TyLeHoanThanhPct", 5, 2), ("BaoCaoTienDo", "DiemTienDo", 3, 1),
        ("YKienGVHD", "DiemGVHD", 3, 1), ("YKienGVHD", "DiemKhac", 3, 1),
        ("KetQuaDocDuyet", "DiemGVDD", 3, 1),
        ("KetQuaKiemTraDaoVan", "TyLeTrungPct", 5, 2), ("KetQuaKiemTraDaoVan", "NguongApDungPct", 5, 2),
        ("LichBaoVe", "TruDiemViPhamPct", 4, 1),
        ("PhieuDiem", "Diem", 3, 1),
        ("BienBan", "ChenhLechMax", 3, 1),
        ("DiemDATN", "DiemGVHD", 3, 1), ("DiemDATN", "DiemGVDD", 3, 1), ("DiemDATN", "DiemKhac", 3, 1),
        ("DiemDATN", "DiemQuaTrinh", 3, 1), ("DiemDATN", "DiemBaoVe", 3, 1), ("DiemDATN", "TrongSoQT", 3, 2),
        ("DiemDATN", "TrongSoHD", 3, 2), ("DiemDATN", "DiemDATN", 3, 1), ("DiemDATN", "DiemHe4", 2, 1),
    ];

    [Fact]
    public async Task Migration_InitialSchema_da_ap_dung_va_khong_con_migration_cho()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var ct = TestContext.Current.CancellationToken;

        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(ct), m => m.EndsWith("_InitialSchema", StringComparison.Ordinal));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
    }

    [Fact]
    public async Task Co_du_46_bang_thiet_ke_va_bang_bo_sung()
    {
        var actual = await QueryAsync(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'",
            r => r.GetString(0));

        Assert.Equal(46 + 1, ExpectedTables.Length);
        Assert.Equal(ExpectedTables.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Khong_co_cot_float_hoac_real()
    {
        var floats = await QueryAsync(
            "SELECT TABLE_NAME + '.' + COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND DATA_TYPE IN ('float', 'real')",
            r => r.GetString(0));

        Assert.Empty(floats);
    }

    [Fact]
    public async Task Moi_cot_decimal_dung_precision_scale_thiet_ke()
    {
        var actual = await QueryAsync(
            "SELECT TABLE_NAME, COLUMN_NAME, CAST(NUMERIC_PRECISION AS int), CAST(NUMERIC_SCALE AS int) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND DATA_TYPE = 'decimal'",
            r => (r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3)));

        // So khớp toàn bộ: không thiếu cột, không có cột decimal(18,2) mặc định ngoài ý muốn.
        Assert.Equal(ExpectedDecimals.Order(), actual.Order());
    }

    [Theory]
    [InlineData("CoSo", "IX_CoSo_MaCoSo")]
    [InlineData("GiangVien", "IX_GiangVien_MaGV")]
    [InlineData("SinhVien", "IX_SinhVien_MaSV")]
    [InlineData("TaiKhoan", "IX_TaiKhoan_TenDangNhap")]
    [InlineData("VaiTro", "IX_VaiTro_MaVaiTro")]
    [InlineData("HocPhan", "IX_HocPhan_MaHP")]
    [InlineData("DotDATN", "IX_DotDATN_MaDot")]
    [InlineData("SVTrongDot", "IX_SVTrongDot_DotId_SinhVienId_CTDTId")]
    [InlineData("DiemDATN", "IX_DiemDATN_SVTrongDotId")]
    [InlineData("ThongTinTienDoSV", "IX_ThongTinTienDoSV_SinhVienId")]
    public async Task Unique_index_theo_UK_thiet_ke(string table, string index)
    {
        var isUnique = await QueryAsync(
            $"SELECT i.is_unique FROM sys.indexes i WHERE i.object_id = OBJECT_ID('dbo.{table}') AND i.name = '{index}'",
            r => r.GetBoolean(0));

        Assert.Equal([true], isUnique);
    }

    [Fact]
    public async Task SVTrongDot_RowVersion_la_rowversion()
    {
        var type = await QueryAsync(
            "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SVTrongDot' AND COLUMN_NAME = 'RowVersion'",
            r => r.GetString(0));

        Assert.Equal(["timestamp"], type); // rowversion hiển thị là timestamp trong INFORMATION_SCHEMA
    }

    [Fact]
    public async Task Moi_khoa_ngoai_deu_khong_xoa_day_chuyen()
    {
        var cascades = await QueryAsync(
            // Chỉ xét bảng nghiệp vụ (dbo); schema HangFire do thư viện tự quản lý.
            "SELECT name FROM sys.foreign_keys WHERE delete_referential_action <> 0 AND OBJECT_SCHEMA_NAME(parent_object_id) = 'dbo'",
            r => r.GetString(0));

        Assert.Empty(cascades);
    }

    [Fact]
    public async Task Trung_ma_hoc_phan_bi_unique_index_chan()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var ct = TestContext.Current.CancellationToken;

        db.HocPhan.Add(new HocPhan { MaHP = "UT-DUP-01", TenHP = "Học phần test 1", SoTC = 3 });
        await db.SaveChangesAsync(ct);

        db.ChangeTracker.Clear();
        db.HocPhan.Add(new HocPhan { MaHP = "UT-DUP-01", TenHP = "Học phần test 2", SoTC = 2 });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
        Assert.Equal(2601, Assert.IsType<SqlException>(ex.InnerException).Number); // duplicate key (unique index)
    }

    [Fact]
    public async Task Du_lieu_tham_chieu_co_san_sau_migration()
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(["HCM", "HN"], await db.CoSo.Select(x => x.MaCoSo).OrderBy(x => x).ToListAsync(ct));
        Assert.Equal(
            ["GiangVien", "GiaoVu", "LanhDaoKhoa", "PhongDaoTao", "QuanTri", "SinhVien", "ThuVien", "TruongBoMon"],
            await db.VaiTro.Select(x => x.MaVaiTro).OrderBy(x => x).ToListAsync(ct));

        var rs2021 = await db.RuleSet.SingleAsync(x => x.MaRuleSet == "UTC-2021", ct);
        Assert.Equal(10, rs2021.MaxTCChuaTichLuy);
        Assert.Equal(1.90m, rs2021.GpaBaoVeMin);
        Assert.Equal(2.00m, rs2021.GpaTotNghiepMin);
        Assert.Equal(5.5m, rs2021.DiemDatDATN);
        Assert.Equal(0.30m, rs2021.TrongSoQT);

        // RuleSet theo KHÓA (CLAUDE.md §5.2): K≤65 và K66 → UTC-2021 (K66 cần xác nhận – O2), K≥67 → UTC-2026.
        var map = await (from k in db.RuleSetKhoaHoc
                         join r in db.RuleSet on k.RuleSetId equals r.Id
                         orderby k.KhoaHocTu
                         select new { k.KhoaHocTu, k.KhoaHocDen, k.CanXacNhan, r.MaRuleSet }).ToListAsync(ct);
        Assert.Collection(map,
            m => Assert.Equal((65, false, "UTC-2021"), (m.KhoaHocDen!.Value, m.CanXacNhan, m.MaRuleSet)),
            m => Assert.Equal((66, 66, true, "UTC-2021"), (m.KhoaHocTu, m.KhoaHocDen!.Value, m.CanXacNhan, m.MaRuleSet)),
            m => Assert.Equal((67, (int?)null, "UTC-2026"), (m.KhoaHocTu, m.KhoaHocDen, m.MaRuleSet)));
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var conn = new SqlConnection(factory.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(map(reader));
        }
        return result;
    }
}
