using Microsoft.EntityFrameworkCore;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Infrastructure.Persistence.Conventions;

namespace Upm.Infrastructure.Persistence;

/// <summary>
/// DbContext chính. Bảng/cột theo docs/thiet-ke.md (ERD-01 … ERD-07); cấu hình từng bảng nằm ở
/// <c>Persistence/Configurations</c> (IEntityTypeConfiguration) và được nạp tự động.
/// </summary>
public class UpmDbContext(DbContextOptions<UpmDbContext> options) : DbContext(options)
{
    // ERD-01 · Tổ chức & nhân sự
    public DbSet<CoSo> CoSo => Set<CoSo>();
    public DbSet<Khoa> Khoa => Set<Khoa>();
    public DbSet<BoMon> BoMon => Set<BoMon>();
    public DbSet<GiangVien> GiangVien => Set<GiangVien>();
    public DbSet<SinhVien> SinhVien => Set<SinhVien>();
    public DbSet<ChuongTrinhDaoTao> ChuongTrinhDaoTao => Set<ChuongTrinhDaoTao>();
    public DbSet<TaiKhoan> TaiKhoan => Set<TaiKhoan>();
    public DbSet<VaiTro> VaiTro => Set<VaiTro>();
    public DbSet<TaiKhoanVaiTro> TaiKhoanVaiTro => Set<TaiKhoanVaiTro>();

    // ERD-02 · Cấu hình & dữ liệu đồng bộ
    public DbSet<RuleSet> RuleSet => Set<RuleSet>();
    public DbSet<RuleSetKhoaHoc> RuleSetKhoaHoc => Set<RuleSetKhoaHoc>();
    public DbSet<ThamSoBoMon> ThamSoBoMon => Set<ThamSoBoMon>();
    public DbSet<ThamSoDaoVan> ThamSoDaoVan => Set<ThamSoDaoVan>();
    public DbSet<HocPhan> HocPhan => Set<HocPhan>();
    public DbSet<CTDTHocPhan> CTDTHocPhan => Set<CTDTHocPhan>();
    public DbSet<ThongTinTienDoSV> ThongTinTienDoSV => Set<ThongTinTienDoSV>();
    public DbSet<BangDiem> BangDiem => Set<BangDiem>();
    public DbSet<KyLuatSV> KyLuatSV => Set<KyLuatSV>();

    // ERD-03 · Đợt & xét điều kiện giao
    public DbSet<DotDATN> DotDATN => Set<DotDATN>();
    public DbSet<SVTrongDot> SVTrongDot => Set<SVTrongDot>();
    public DbSet<XetDieuKien> XetDieuKien => Set<XetDieuKien>();
    public DbSet<XetDieuKienChiTiet> XetDieuKienChiTiet => Set<XetDieuKienChiTiet>();

    // ERD-04 · Đề tài, phân công, tiến độ
    public DbSet<MocTienDo> MocTienDo => Set<MocTienDo>();
    public DbSet<DeTai> DeTai => Set<DeTai>();
    public DbSet<BaoCaoTienDo> BaoCaoTienDo => Set<BaoCaoTienDo>();
    public DbSet<DangKyNguyenVong> DangKyNguyenVong => Set<DangKyNguyenVong>();
    public DbSet<PhanCong> PhanCong => Set<PhanCong>();
    public DbSet<KhaiBaoXungDot> KhaiBaoXungDot => Set<KhaiBaoXungDot>();
    public DbSet<TepDinhKem> TepDinhKem => Set<TepDinhKem>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();
    public DbSet<LichNhacViec> LichNhacViec => Set<LichNhacViec>();
    public DbSet<PhieuGiaoNhiemVu> PhieuGiaoNhiemVu => Set<PhieuGiaoNhiemVu>();

    // ERD-05 · Quyển, đọc duyệt, đạo văn
    public DbSet<PhienBanQuyen> PhienBanQuyen => Set<PhienBanQuyen>();
    public DbSet<YKienGVHD> YKienGVHD => Set<YKienGVHD>();
    public DbSet<KetQuaDocDuyet> KetQuaDocDuyet => Set<KetQuaDocDuyet>();
    public DbSet<KetQuaKiemTraDaoVan> KetQuaKiemTraDaoVan => Set<KetQuaKiemTraDaoVan>();
    public DbSet<LuuTruThuVien> LuuTruThuVien => Set<LuuTruThuVien>();

    // ERD-06 · Hội đồng, bảo vệ, điểm
    public DbSet<HoiDong> HoiDong => Set<HoiDong>();
    public DbSet<ThanhVienHoiDong> ThanhVienHoiDong => Set<ThanhVienHoiDong>();
    public DbSet<PhongBaoVe> PhongBaoVe => Set<PhongBaoVe>();
    public DbSet<LichBaoVe> LichBaoVe => Set<LichBaoVe>();
    public DbSet<PhieuDiem> PhieuDiem => Set<PhieuDiem>();
    public DbSet<BienBan> BienBan => Set<BienBan>();
    public DbSet<DiemDATN> DiemDATN => Set<DiemDATN>();

    // ERD-07 · Quyết định, đơn từ, log
    public DbSet<QuyetDinh> QuyetDinh => Set<QuyetDinh>();
    public DbSet<QuyetDinhDoiTuong> QuyetDinhDoiTuong => Set<QuyetDinhDoiTuong>();
    public DbSet<DonTu> DonTu => Set<DonTu>();
    public DbSet<LichSuTrangThai> LichSuTrangThai => Set<LichSuTrangThai>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    // Bổ sung Phần 3 · Xác thực
    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enum lưu dạng chuỗi (CLAUDE.md §6); 30 ký tự đủ cho tên dài nhất (KHONG_DU_DK_CHUYEN_THAY_THE).
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        // datetime2 luôn là UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        // date → DateOnly (EF Core 8+ hỗ trợ sẵn).
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpmDbContext).Assembly);
        CauHinhCotAudit(modelBuilder);
    }

    /// <summary>
    /// Cột audit chung cho mọi entity <see cref="IAuditable"/> (quyết định BA – Phần 3b):
    /// NgayTao mặc định SYSUTCDATETIME() (dòng cũ khi migrate), NguoiTaoId/NguoiSuaId FK TaiKhoan (Restrict, không navigation).
    /// </summary>
    private static void CauHinhCotAudit(ModelBuilder modelBuilder)
    {
        var loaiAudit = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IAuditable).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clr in loaiAudit)
        {
            var b = modelBuilder.Entity(clr);
            b.Property(nameof(IAuditable.NgayTao)).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(typeof(TaiKhoan)).WithMany().HasForeignKey(nameof(IAuditable.NguoiTaoId)).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(typeof(TaiKhoan)).WithMany().HasForeignKey(nameof(IAuditable.NguoiSuaId)).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
