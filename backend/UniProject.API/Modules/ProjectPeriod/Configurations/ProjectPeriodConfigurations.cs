using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.ProjectPeriod
{
    public class DotDoAnConfiguration : IEntityTypeConfiguration<DotDoAn>
    {
        public void Configure(EntityTypeBuilder<DotDoAn> builder)
        {
            builder.ToTable("DOTDOAN", table =>
            {
                table.HasCheckConstraint("CK_DOTDOAN_NgayThucHien", "[NgayKetThuc] >= [NgayBatDau]");
                table.HasCheckConstraint("CK_DOTDOAN_NgayBaoVe", "[NgayKetThucBaoVe] >= [NgayBatDauBaoVe]");
            });

            builder.HasKey(x => x.Id).HasName("PK_DOTDOAN");
            builder.Property(x => x.Id).HasColumnName("MaDotDoAn").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDotDoAnCode).HasColumnName("MaDotDoAnCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenDotDoAn).HasColumnName("TenDotDoAn").HasMaxLength(150).IsRequired();
            builder.Property(x => x.LoaiDoAn).HasColumnName("LoaiDoAn").HasMaxLength(30).HasConversion<string>().IsRequired();
            builder.Property(x => x.MaHocKy).HasColumnName("MaHocKy").IsRequired();

            builder.Property(x => x.NgayBatDau).HasColumnName("NgayBatDau").IsRequired();
            builder.Property(x => x.NgayKetThuc).HasColumnName("NgayKetThuc").IsRequired();
            builder.Property(x => x.HanDangKyDeTai).HasColumnName("HanDangKyDeTai").IsRequired();
            builder.Property(x => x.HanNopDeCuong).HasColumnName("HanNopDeCuong").IsRequired();
            builder.Property(x => x.HanNopBaoCao).HasColumnName("HanNopBaoCao").IsRequired();
            builder.Property(x => x.NgayBatDauBaoVe).HasColumnName("NgayBatDauBaoVe").IsRequired();
            builder.Property(x => x.NgayKetThucBaoVe).HasColumnName("NgayKetThucBaoVe").IsRequired();

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

            builder.HasIndex(x => x.MaDotDoAnCode).IsUnique().HasDatabaseName("UQ_DOTDOAN_MaDotDoAnCode");
            builder.HasIndex(x => x.MaHocKy).HasDatabaseName("IX_DOTDOAN_MaHocKy");
            builder.HasIndex(x => x.LoaiDoAn).HasDatabaseName("IX_DOTDOAN_LoaiDoAn");
        }
    }

    public class DotDoAnLopConfiguration : IEntityTypeConfiguration<DotDoAnLop>
    {
        public void Configure(EntityTypeBuilder<DotDoAnLop> builder)
        {
            builder.ToTable("DOTDOAN_LOP");

            builder.HasKey(x => x.Id).HasName("PK_DOTDOAN_LOP");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();
            builder.Property(x => x.MaLop).HasColumnName("MaLop").IsRequired();

            builder.HasOne(x => x.DotDoAn)
                .WithMany(x => x.DotDoAnLops)
                .HasForeignKey(x => x.MaDotDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DOTDOANLOP_DOTDOAN");

            builder.HasIndex(x => new { x.MaDotDoAn, x.MaLop })
                .IsUnique()
                .HasDatabaseName("UQ_DOTDOANLOP_MaDotDoAn_MaLop");
        }
    }
}
