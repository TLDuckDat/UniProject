using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Identity
{
    public class TaiKhoanConfiguration : IEntityTypeConfiguration<TaiKhoan>
    {
        public void Configure(EntityTypeBuilder<TaiKhoan> builder)
        {
            builder.ToTable("TAIKHOAN");

            builder.HasKey(x => x.Id)
                .HasName("PK_TAIKHOAN");

            builder.Property(x => x.Id)
                .HasColumnName("MaTaiKhoan")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.TenDangNhap)
                .HasColumnName("TenDangNhap")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.MatKhauHash)
                .HasColumnName("MatKhauHash")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("Email")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.HoTen)
                .HasColumnName("HoTen")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.MaVaiTro)
                .HasColumnName("MaVaiTro")
                .IsRequired();

            builder.Property(x => x.DaKhoa)
                .HasColumnName("DaKhoa")
                .HasDefaultValue(false);

            builder.Property(x => x.RefreshToken)
                .HasColumnName("RefreshToken")
                .HasMaxLength(500);

            builder.Property(x => x.RefreshTokenExpiryTime)
                .HasColumnName("RefreshTokenExpiryTime");

            builder.Property(x => x.LanDangNhapCuoi)
                .HasColumnName("LanDangNhapCuoi");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("NgayTao")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.CreatedBy)
                .HasColumnName("NguoiTao")
                .HasMaxLength(50);

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("NgayCapNhat");

            builder.Property(x => x.UpdatedBy)
                .HasColumnName("NguoiCapNhat")
                .HasMaxLength(50);

            builder.Property(x => x.IsActive)
                .HasColumnName("TrangThai")
                .HasDefaultValue(true);

            builder.Property(x => x.RowVersion)
                .HasColumnName("RowVersion")
                .IsRowVersion();

            builder.HasOne(x => x.VaiTro)
                .WithMany(x => x.TaiKhoans)
                .HasForeignKey(x => x.MaVaiTro)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TAIKHOAN_VAITRO");

            builder.HasIndex(x => x.TenDangNhap)
                .IsUnique()
                .HasDatabaseName("UQ_TAIKHOAN_TenDangNhap");

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("UQ_TAIKHOAN_Email");

            builder.HasIndex(x => x.MaVaiTro)
                .HasDatabaseName("IX_TAIKHOAN_MaVaiTro");
        }
    }
}
