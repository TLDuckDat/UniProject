using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Academic
{
    public class NamHocConfiguration : IEntityTypeConfiguration<NamHoc>
    {
        public void Configure(EntityTypeBuilder<NamHoc> builder)
        {
            builder.ToTable("NAMHOC", table =>
            {
                table.HasCheckConstraint("CK_NAMHOC_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
            });

            builder.HasKey(x => x.Id).HasName("PK_NAMHOC");
            builder.Property(x => x.Id).HasColumnName("MaNamHoc").ValueGeneratedOnAdd();
            builder.Property(x => x.TenNamHoc).HasColumnName("TenNamHoc").HasMaxLength(50).IsRequired();
            builder.Property(x => x.NgayBatDau).HasColumnName("NgayBatDau").IsRequired();
            builder.Property(x => x.NgayKetThuc).HasColumnName("NgayKetThuc").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasIndex(x => x.TenNamHoc).IsUnique().HasDatabaseName("UQ_NAMHOC_TenNamHoc");
        }
    }

    public class HocKyConfiguration : IEntityTypeConfiguration<HocKy>
    {
        public void Configure(EntityTypeBuilder<HocKy> builder)
        {
            builder.ToTable("HOCKY", table =>
            {
                table.HasCheckConstraint("CK_HOCKY_HocKySo", "[HocKySo] BETWEEN 1 AND 3");
                table.HasCheckConstraint("CK_HOCKY_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
            });

            builder.HasKey(x => x.Id).HasName("PK_HOCKY");
            builder.Property(x => x.Id).HasColumnName("MaHocKy").ValueGeneratedOnAdd();
            builder.Property(x => x.TenHocKy).HasColumnName("TenHocKy").HasMaxLength(50).IsRequired();
            builder.Property(x => x.HocKySo).HasColumnName("HocKySo").IsRequired();
            builder.Property(x => x.MaNamHoc).HasColumnName("MaNamHoc").IsRequired();
            builder.Property(x => x.NgayBatDau).HasColumnName("NgayBatDau").IsRequired();
            builder.Property(x => x.NgayKetThuc).HasColumnName("NgayKetThuc").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.NamHoc)
                .WithMany(x => x.HocKys)
                .HasForeignKey(x => x.MaNamHoc)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HOCKY_NAMHOC");

            builder.HasIndex(x => new { x.HocKySo, x.MaNamHoc }).IsUnique().HasDatabaseName("UQ_HOCKY_HocKySo_MaNamHoc");
            builder.HasIndex(x => x.MaNamHoc).HasDatabaseName("IX_HOCKY_MaNamHoc");
        }
    }

    public class HocViConfiguration : IEntityTypeConfiguration<HocVi>
    {
        public void Configure(EntityTypeBuilder<HocVi> builder)
        {
            builder.ToTable("HOCVI");

            builder.HasKey(x => x.Id).HasName("PK_HOCVI");
            builder.Property(x => x.Id).HasColumnName("MaHocVi").ValueGeneratedNever();
            builder.Property(x => x.TenHocVi).HasColumnName("TenHocVi").HasMaxLength(50).IsRequired();

            builder.HasIndex(x => x.TenHocVi).IsUnique().HasDatabaseName("UQ_HOCVI_TenHocVi");

            builder.HasData(
                new HocVi(1, "Cử nhân"),
                new HocVi(2, "Kỹ sư"),
                new HocVi(3, "Thạc sĩ"),
                new HocVi(4, "Tiến sĩ")
            );
        }
    }

    public class HocHamConfiguration : IEntityTypeConfiguration<HocHam>
    {
        public void Configure(EntityTypeBuilder<HocHam> builder)
        {
            builder.ToTable("HOCHAM");

            builder.HasKey(x => x.Id).HasName("PK_HOCHAM");
            builder.Property(x => x.Id).HasColumnName("MaHocHam").ValueGeneratedNever();
            builder.Property(x => x.TenHocHam).HasColumnName("TenHocHam").HasMaxLength(50).IsRequired();

            builder.HasIndex(x => x.TenHocHam).IsUnique().HasDatabaseName("UQ_HOCHAM_TenHocHam");

            builder.HasData(
                new HocHam(1, "Phó Giáo sư"),
                new HocHam(2, "Giáo sư")
            );
        }
    }

    public class GiangVienConfiguration : IEntityTypeConfiguration<GiangVien>
    {
        public void Configure(EntityTypeBuilder<GiangVien> builder)
        {
            builder.ToTable("GIANGVIEN");

            builder.HasKey(x => x.Id).HasName("PK_GIANGVIEN");
            builder.Property(x => x.Id).HasColumnName("MaGV").ValueGeneratedOnAdd();
            builder.Property(x => x.MaGVCode).HasColumnName("MaGVCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.HoTen).HasColumnName("HoTen").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Email).HasColumnName("Email").HasMaxLength(100).IsRequired();
            builder.Property(x => x.SoDienThoai).HasColumnName("SoDienThoai").HasMaxLength(20);
            builder.Property(x => x.MaBoMon).HasColumnName("MaBoMon").IsRequired();
            builder.Property(x => x.MaHocVi).HasColumnName("MaHocVi").IsRequired();
            builder.Property(x => x.MaHocHam).HasColumnName("MaHocHam");
            builder.Property(x => x.MaTaiKhoan).HasColumnName("MaTaiKhoan").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.BoMon)
                .WithMany(x => x.GiangViens)
                .HasForeignKey(x => x.MaBoMon)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GIANGVIEN_BOMON");

            builder.HasOne(x => x.HocVi)
                .WithMany(x => x.GiangViens)
                .HasForeignKey(x => x.MaHocVi)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GIANGVIEN_HOCVI");

            builder.HasOne(x => x.HocHam)
                .WithMany(x => x.GiangViens)
                .HasForeignKey(x => x.MaHocHam)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GIANGVIEN_HOCHAM");

            builder.HasIndex(x => x.MaGVCode).IsUnique().HasDatabaseName("UQ_GIANGVIEN_MaGVCode");
            builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_GIANGVIEN_Email");
            builder.HasIndex(x => x.MaTaiKhoan).IsUnique().HasDatabaseName("UQ_GIANGVIEN_MaTaiKhoan");
            builder.HasIndex(x => x.MaBoMon).HasDatabaseName("IX_GIANGVIEN_MaBoMon");
        }
    }

    public class SinhVienConfiguration : IEntityTypeConfiguration<SinhVien>
    {
        public void Configure(EntityTypeBuilder<SinhVien> builder)
        {
            builder.ToTable("SINHVIEN");

            builder.HasKey(x => x.Id).HasName("PK_SINHVIEN");
            builder.Property(x => x.Id).HasColumnName("MaSV").ValueGeneratedOnAdd();
            builder.Property(x => x.MaSVCode).HasColumnName("MaSVCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.HoTen).HasColumnName("HoTen").HasMaxLength(100).IsRequired();
            builder.Property(x => x.NgaySinh).HasColumnName("NgaySinh");
            builder.Property(x => x.GioiTinh).HasColumnName("GioiTinh").HasMaxLength(10);
            builder.Property(x => x.Email).HasColumnName("Email").HasMaxLength(100).IsRequired();
            builder.Property(x => x.SoDienThoai).HasColumnName("SoDienThoai").HasMaxLength(20);
            builder.Property(x => x.MaLop).HasColumnName("MaLop").IsRequired();
            builder.Property(x => x.MaTaiKhoan).HasColumnName("MaTaiKhoan").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.Lop)
                .WithMany(x => x.SinhViens)
                .HasForeignKey(x => x.MaLop)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SINHVIEN_LOP");

            builder.HasIndex(x => x.MaSVCode).IsUnique().HasDatabaseName("UQ_SINHVIEN_MaSVCode");
            builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_SINHVIEN_Email");
            builder.HasIndex(x => x.MaTaiKhoan).IsUnique().HasDatabaseName("UQ_SINHVIEN_MaTaiKhoan");
            builder.HasIndex(x => x.MaLop).HasDatabaseName("IX_SINHVIEN_MaLop");
        }
    }
}
