using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Topic
{
    public class DeTaiConfiguration : IEntityTypeConfiguration<DeTai>
    {
        public void Configure(EntityTypeBuilder<DeTai> builder)
        {
            builder.ToTable("DETAI", table =>
            {
                table.HasCheckConstraint("CK_DETAI_SoLuongSinhVien", "[SoLuongSinhVienToiDa] BETWEEN 1 AND 10");
            });

            builder.HasKey(x => x.Id).HasName("PK_DETAI");
            builder.Property(x => x.Id).HasColumnName("MaDeTai").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDeTaiCode).HasColumnName("MaDeTaiCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenDeTai).HasColumnName("TenDeTai").HasMaxLength(250).IsRequired();
            builder.Property(x => x.MoTa).HasColumnName("MoTa").IsRequired();
            builder.Property(x => x.MucTieu).HasColumnName("MucTieu");
            builder.Property(x => x.YeuCau).HasColumnName("YeuCau");
            builder.Property(x => x.CongNgheDuKien).HasColumnName("CongNgheDuKien").HasMaxLength(250);
            builder.Property(x => x.SoLuongSinhVienToiDa).HasColumnName("SoLuongSinhVienToiDa").IsRequired();
            builder.Property(x => x.MaGVDeXuat).HasColumnName("MaGVDeXuat").IsRequired();
            builder.Property(x => x.MaSVDeXuat).HasColumnName("MaSVDeXuat");
            builder.Property(x => x.MaChuyenNganh).HasColumnName("MaChuyenNganh").IsRequired();
            builder.Property(x => x.LoaiDoAn).HasColumnName("LoaiDoAn").HasMaxLength(30).HasConversion<string>().IsRequired();
            builder.Property(x => x.MaGVDuyet).HasColumnName("MaGVDuyet");
            builder.Property(x => x.NgayDuyet).HasColumnName("NgayDuyet");
            builder.Property(x => x.LyDoTuChoi).HasColumnName("LyDoTuChoi").HasMaxLength(500);

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

            builder.HasIndex(x => x.MaDeTaiCode).IsUnique().HasDatabaseName("UQ_DETAI_MaDeTaiCode");
            builder.HasIndex(x => x.MaGVDeXuat).HasDatabaseName("IX_DETAI_MaGVDeXuat");
            builder.HasIndex(x => x.MaSVDeXuat).HasDatabaseName("IX_DETAI_MaSVDeXuat");
            builder.HasIndex(x => x.MaChuyenNganh).HasDatabaseName("IX_DETAI_MaChuyenNganh");
        }
    }

    public class DeTaiDotDoAnConfiguration : IEntityTypeConfiguration<DeTaiDotDoAn>
    {
        public void Configure(EntityTypeBuilder<DeTaiDotDoAn> builder)
        {
            builder.ToTable("DETAI_DOTDOAN");

            builder.HasKey(x => x.Id).HasName("PK_DETAI_DOTDOAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDeTai).HasColumnName("MaDeTai").IsRequired();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.DeTai)
                .WithMany(x => x.DeTaiDotDoAns)
                .HasForeignKey(x => x.MaDeTai)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DETAIDOTDOAN_DETAI");

            builder.HasIndex(x => new { x.MaDeTai, x.MaDotDoAn })
                .IsUnique()
                .HasDatabaseName("UQ_DETAIDOTDOAN_MaDeTai_MaDotDoAn");

            builder.HasIndex(x => x.MaDotDoAn).HasDatabaseName("IX_DETAIDOTDOAN_MaDotDoAn");
        }
    }
}
