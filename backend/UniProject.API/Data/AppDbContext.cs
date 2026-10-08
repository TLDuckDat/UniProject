using Microsoft.EntityFrameworkCore;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Identity;
using UniProject.API.Modules.Notification;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;

namespace UniProject.API.Data
{
    /// <summary>
    /// DbContext dùng chung cho mọi module. Cấu hình bảng nằm ở Modules/*/Configurations
    /// và được nạp tự động trong OnModelCreating.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
        public DbSet<VaiTro> VaiTros => Set<VaiTro>();

        public DbSet<Khoa> Khoas => Set<Khoa>();
        public DbSet<BoMon> BoMons => Set<BoMon>();
        public DbSet<ChuyenNganh> ChuyenNganhs => Set<ChuyenNganh>();
        public DbSet<Lop> Lops => Set<Lop>();
        public DbSet<NamHoc> NamHocs => Set<NamHoc>();
        public DbSet<HocKy> HocKys => Set<HocKy>();
        public DbSet<HocVi> HocVis => Set<HocVi>();
        public DbSet<HocHam> HocHams => Set<HocHam>();
        public DbSet<GiangVien> GiangViens => Set<GiangVien>();
        public DbSet<SinhVien> SinhViens => Set<SinhVien>();

        public DbSet<DotDoAn> DotDoAns => Set<DotDoAn>();
        public DbSet<DotDoAnLop> DotDoAnLops => Set<DotDoAnLop>();

        public DbSet<DeTai> DeTais => Set<DeTai>();
        public DbSet<DeTaiDotDoAn> DeTaiDotDoAns => Set<DeTaiDotDoAn>();

        public DbSet<DangKyDeTai> DangKyDeTais => Set<DangKyDeTai>();
        public DbSet<ThanhVienDangKy> ThanhVienDangKys => Set<ThanhVienDangKy>();
        public DbSet<ChiTietDangKy> ChiTietDangKys => Set<ChiTietDangKy>();
        public DbSet<YeuCauThayDoi> YeuCauThayDois => Set<YeuCauThayDoi>();

        public DbSet<DoAn> DoAns => Set<DoAn>();
        public DbSet<ThanhVienDoAn> ThanhVienDoAns => Set<ThanhVienDoAn>();
        public DbSet<PhanCongHuongDan> PhanCongHuongDans => Set<PhanCongHuongDan>();
        public DbSet<BaoCaoTienDo> BaoCaoTienDos => Set<BaoCaoTienDo>();
        public DbSet<NhanXetTienDo> NhanXetTienDos => Set<NhanXetTienDo>();
        public DbSet<YeuCauChinhSua> YeuCauChinhSuas => Set<YeuCauChinhSua>();
        public DbSet<TaiLieuDoAn> TaiLieuDoAns => Set<TaiLieuDoAn>();

        public DbSet<PhanBienDoAn> PhanBienDoAns => Set<PhanBienDoAn>();
        public DbSet<HoiDong> HoiDongs => Set<HoiDong>();
        public DbSet<ThanhVienHoiDong> ThanhVienHoiDongs => Set<ThanhVienHoiDong>();
        public DbSet<LichBaoVe> LichBaoVes => Set<LichBaoVe>();

        public DbSet<PhieuCham> PhieuChams => Set<PhieuCham>();
        public DbSet<KetQuaDoAn> KetQuaDoAns => Set<KetQuaDoAn>();

        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<NguoiNhanThongBao> NguoiNhanThongBaos => Set<NguoiNhanThongBao>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            CrossModuleRelationships.Apply(modelBuilder);

            // Mọi entity có cột RowVersion (AuditableEntity) đều dùng làm concurrency token:
            // hai người sửa cùng lúc thì người lưu sau nhận 409 thay vì ghi đè âm thầm.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion?.ClrType == typeof(byte[]))
                    modelBuilder.Entity(entityType.ClrType).Property("RowVersion").HasColumnName("RowVersion").IsRowVersion();
            }
        }
    }
}
