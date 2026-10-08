using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Grading
{
    public class PhieuChamConfiguration : IEntityTypeConfiguration<PhieuCham>
    {
        public void Configure(EntityTypeBuilder<PhieuCham> builder)
        {
            builder.ToTable("PHIEUCHAM", table =>
            {
                table.HasCheckConstraint("CK_PHIEUCHAM_Diem", "[Diem] BETWEEN 0 AND 10");
            });

            builder.HasKey(x => x.Id).HasName("PK_PHIEUCHAM");
            builder.Property(x => x.Id).HasColumnName("MaPhieuCham").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaSV).HasColumnName("MaSV").IsRequired();
            builder.Property(x => x.MaGV).HasColumnName("MaGV").IsRequired();
            builder.Property(x => x.MaHoiDong).HasColumnName("MaHoiDong");
            builder.Property(x => x.Diem).HasColumnName("Diem").HasPrecision(4, 2).IsRequired();
            builder.Property(x => x.NhanXet).HasColumnName("NhanXet").HasMaxLength(1000);
            builder.Property(x => x.NgayCham).HasColumnName("NgayCham").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.LoaiCham)
                .HasColumnName("LoaiCham")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => new { x.MaDoAn, x.MaSV, x.MaGV, x.LoaiCham })
                .IsUnique()
                .HasDatabaseName("UQ_PHIEUCHAM_DoAn_SV_GV_LoaiCham");

            builder.HasIndex(x => new { x.MaDoAn, x.MaSV }).HasDatabaseName("IX_PHIEUCHAM_MaDoAn_MaSV");
            builder.HasIndex(x => x.MaGV).HasDatabaseName("IX_PHIEUCHAM_MaGV");
        }
    }

    public class KetQuaDoAnConfiguration : IEntityTypeConfiguration<KetQuaDoAn>
    {
        public void Configure(EntityTypeBuilder<KetQuaDoAn> builder)
        {
            builder.ToTable("KETQUA_DOAN");

            builder.HasKey(x => x.Id).HasName("PK_KETQUA_DOAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaSV).HasColumnName("MaSV").IsRequired();
            builder.Property(x => x.DiemHuongDan).HasColumnName("DiemHuongDan").HasPrecision(4, 2);
            builder.Property(x => x.DiemPhanBien).HasColumnName("DiemPhanBien").HasPrecision(4, 2);
            builder.Property(x => x.DiemHoiDong).HasColumnName("DiemHoiDong").HasPrecision(4, 2);
            builder.Property(x => x.DiemTongKet).HasColumnName("DiemTongKet").HasPrecision(4, 2);
            builder.Property(x => x.NgayCongBo).HasColumnName("NgayCongBo");

            builder.Property(x => x.XepLoai)
                .HasColumnName("XepLoai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.KetQua)
                .HasColumnName("KetQua")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => new { x.MaDoAn, x.MaSV })
                .IsUnique()
                .HasDatabaseName("UQ_KETQUADOAN_MaDoAn_MaSV");
        }
    }
}
