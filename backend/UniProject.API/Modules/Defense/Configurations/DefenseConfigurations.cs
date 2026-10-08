using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Defense
{
    public class PhanBienDoAnConfiguration : IEntityTypeConfiguration<PhanBienDoAn>
    {
        public void Configure(EntityTypeBuilder<PhanBienDoAn> builder)
        {
            builder.ToTable("PHANBIEN_DOAN");

            builder.HasKey(x => x.Id).HasName("PK_PHANBIEN_DOAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaGV).HasColumnName("MaGV").IsRequired();
            builder.Property(x => x.NgayPhanCong).HasColumnName("NgayPhanCong").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasIndex(x => new { x.MaDoAn, x.MaGV }).IsUnique().HasDatabaseName("UQ_PHANBIENDOAN_MaDoAn_MaGV");
            builder.HasIndex(x => x.MaGV).HasDatabaseName("IX_PHANBIENDOAN_MaGV");
        }
    }

    public class HoiDongConfiguration : IEntityTypeConfiguration<HoiDong>
    {
        public void Configure(EntityTypeBuilder<HoiDong> builder)
        {
            builder.ToTable("HOIDONG");

            builder.HasKey(x => x.Id).HasName("PK_HOIDONG");
            builder.Property(x => x.Id).HasColumnName("MaHoiDong").ValueGeneratedOnAdd();
            builder.Property(x => x.MaHoiDongCode).HasColumnName("MaHoiDongCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenHoiDong).HasColumnName("TenHoiDong").HasMaxLength(150).IsRequired();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();
            builder.Property(x => x.DiaDiem).HasColumnName("DiaDiem").HasMaxLength(200);
            builder.Property(x => x.NgayTao).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTaoRecord").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

            builder.HasIndex(x => x.MaHoiDongCode).IsUnique().HasDatabaseName("UQ_HOIDONG_MaHoiDongCode");
            builder.HasIndex(x => x.MaDotDoAn).HasDatabaseName("IX_HOIDONG_MaDotDoAn");
        }
    }

    public class ThanhVienHoiDongConfiguration : IEntityTypeConfiguration<ThanhVienHoiDong>
    {
        public void Configure(EntityTypeBuilder<ThanhVienHoiDong> builder)
        {
            builder.ToTable("THANHVIEN_HOIDONG");

            builder.HasKey(x => x.Id).HasName("PK_THANHVIEN_HOIDONG");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaHoiDong).HasColumnName("MaHoiDong").IsRequired();
            builder.Property(x => x.MaGV).HasColumnName("MaGV").IsRequired();

            builder.Property(x => x.VaiTro)
                .HasColumnName("VaiTro")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.HoiDong)
                .WithMany(x => x.ThanhViens)
                .HasForeignKey(x => x.MaHoiDong)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_THANHVIENHOIDONG_HOIDONG");

            builder.HasIndex(x => new { x.MaHoiDong, x.MaGV }).IsUnique().HasDatabaseName("UQ_THANHVIENHOIDONG_MaHoiDong_MaGV");

            // Hai index unique có điều kiện trên cùng cột MaHoiDong phải đặt tên riêng (named index);
            // nếu không, EF gộp thành một index và chỉ giữ cấu hình khai báo sau cùng.
            // Mỗi hội đồng chỉ 1 Chủ tịch
            builder.HasIndex(x => x.MaHoiDong, "UQ_THANHVIENHOIDONG_OneChuTich")
                .IsUnique()
                .HasFilter("[VaiTro] = 'CHU_TICH'")
                .HasDatabaseName("UQ_THANHVIENHOIDONG_OneChuTich");

            // Mỗi hội đồng chỉ 1 Thư ký
            builder.HasIndex(x => x.MaHoiDong, "UQ_THANHVIENHOIDONG_OneThuKy")
                .IsUnique()
                .HasFilter("[VaiTro] = 'THU_KY'")
                .HasDatabaseName("UQ_THANHVIENHOIDONG_OneThuKy");
        }
    }

    public class LichBaoVeConfiguration : IEntityTypeConfiguration<LichBaoVe>
    {
        public void Configure(EntityTypeBuilder<LichBaoVe> builder)
        {
            builder.ToTable("LICH_BAOVE", table =>
            {
                table.HasCheckConstraint("CK_LICHBAOVE_Gio", "[GioKetThuc] > [GioBatDau]");
            });

            builder.HasKey(x => x.Id).HasName("PK_LICH_BAOVE");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaHoiDong).HasColumnName("MaHoiDong").IsRequired();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();
            builder.Property(x => x.NgayBaoVe).HasColumnName("NgayBaoVe").IsRequired();
            builder.Property(x => x.GioBatDau).HasColumnName("GioBatDau").IsRequired();
            builder.Property(x => x.GioKetThuc).HasColumnName("GioKetThuc").IsRequired();
            builder.Property(x => x.Phong).HasColumnName("Phong").HasMaxLength(50).IsRequired();

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.HoiDong)
                .WithMany(x => x.LichBaoVes)
                .HasForeignKey(x => x.MaHoiDong)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_LICHBAOVE_HOIDONG");

            builder.HasIndex(x => new { x.NgayBaoVe, x.Phong, x.GioBatDau, x.GioKetThuc })
                .HasDatabaseName("IX_LICHBAOVE_RoomSchedule");

            builder.HasIndex(x => new { x.NgayBaoVe, x.MaHoiDong, x.GioBatDau, x.GioKetThuc })
                .HasDatabaseName("IX_LICHBAOVE_CommitteeSchedule");
        }
    }
}
