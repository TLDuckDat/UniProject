using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Academic
{
    public class KhoaConfiguration : IEntityTypeConfiguration<Khoa>
    {
        public void Configure(EntityTypeBuilder<Khoa> builder)
        {
            builder.ToTable("KHOA");

            builder.HasKey(x => x.Id).HasName("PK_KHOA");
            builder.Property(x => x.Id).HasColumnName("MaKhoa").ValueGeneratedOnAdd();
            builder.Property(x => x.MaKhoaCode).HasColumnName("MaKhoaCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenKhoa).HasColumnName("TenKhoa").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Email).HasColumnName("Email").HasMaxLength(100);
            builder.Property(x => x.SoDienThoai).HasColumnName("SoDienThoai").HasMaxLength(20);

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasIndex(x => x.MaKhoaCode).IsUnique().HasDatabaseName("UQ_KHOA_MaKhoaCode");
            builder.HasIndex(x => x.TenKhoa).IsUnique().HasDatabaseName("UQ_KHOA_TenKhoa");
        }
    }

    public class BoMonConfiguration : IEntityTypeConfiguration<BoMon>
    {
        public void Configure(EntityTypeBuilder<BoMon> builder)
        {
            builder.ToTable("BOMON");

            builder.HasKey(x => x.Id).HasName("PK_BOMON");
            builder.Property(x => x.Id).HasColumnName("MaBoMon").ValueGeneratedOnAdd();
            builder.Property(x => x.MaBoMonCode).HasColumnName("MaBoMonCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenBoMon).HasColumnName("TenBoMon").HasMaxLength(100).IsRequired();
            builder.Property(x => x.MaKhoa).HasColumnName("MaKhoa").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.Khoa)
                .WithMany(x => x.BoMons)
                .HasForeignKey(x => x.MaKhoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_BOMON_KHOA");

            builder.HasIndex(x => x.MaBoMonCode).IsUnique().HasDatabaseName("UQ_BOMON_MaBoMonCode");
            builder.HasIndex(x => new { x.TenBoMon, x.MaKhoa }).IsUnique().HasDatabaseName("UQ_BOMON_TenBoMon_MaKhoa");
            builder.HasIndex(x => x.MaKhoa).HasDatabaseName("IX_BOMON_MaKhoa");
        }
    }

    public class ChuyenNganhConfiguration : IEntityTypeConfiguration<ChuyenNganh>
    {
        public void Configure(EntityTypeBuilder<ChuyenNganh> builder)
        {
            builder.ToTable("CHUYENNGANH");

            builder.HasKey(x => x.Id).HasName("PK_CHUYENNGANH");
            builder.Property(x => x.Id).HasColumnName("MaChuyenNganh").ValueGeneratedOnAdd();
            builder.Property(x => x.MaChuyenNganhCode).HasColumnName("MaChuyenNganhCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenChuyenNganh).HasColumnName("TenChuyenNganh").HasMaxLength(100).IsRequired();
            builder.Property(x => x.MaBoMon).HasColumnName("MaBoMon").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.BoMon)
                .WithMany(x => x.ChuyenNganhs)
                .HasForeignKey(x => x.MaBoMon)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CHUYENNGANH_BOMON");

            builder.HasIndex(x => x.MaChuyenNganhCode).IsUnique().HasDatabaseName("UQ_CHUYENNGANH_MaChuyenNganhCode");
            builder.HasIndex(x => new { x.TenChuyenNganh, x.MaBoMon }).IsUnique().HasDatabaseName("UQ_CHUYENNGANH_TenChuyenNganh_MaBoMon");
            builder.HasIndex(x => x.MaBoMon).HasDatabaseName("IX_CHUYENNGANH_MaBoMon");
        }
    }

    public class LopConfiguration : IEntityTypeConfiguration<Lop>
    {
        public void Configure(EntityTypeBuilder<Lop> builder)
        {
            builder.ToTable("LOP", table =>
            {
                table.HasCheckConstraint("CK_LOP_NamNhapHoc", "[NamNhapHoc] BETWEEN 2000 AND 2100");
            });

            builder.HasKey(x => x.Id).HasName("PK_LOP");
            builder.Property(x => x.Id).HasColumnName("MaLop").ValueGeneratedOnAdd();
            builder.Property(x => x.MaLopCode).HasColumnName("MaLopCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenLop).HasColumnName("TenLop").HasMaxLength(100).IsRequired();
            builder.Property(x => x.NamNhapHoc).HasColumnName("NamNhapHoc").IsRequired();
            builder.Property(x => x.MaChuyenNganh).HasColumnName("MaChuyenNganh").IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.HasOne(x => x.ChuyenNganh)
                .WithMany(x => x.Lops)
                .HasForeignKey(x => x.MaChuyenNganh)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_LOP_CHUYENNGANH");

            builder.HasIndex(x => x.MaLopCode).IsUnique().HasDatabaseName("UQ_LOP_MaLopCode");
            builder.HasIndex(x => x.MaChuyenNganh).HasDatabaseName("IX_LOP_MaChuyenNganh");
        }
    }
}
