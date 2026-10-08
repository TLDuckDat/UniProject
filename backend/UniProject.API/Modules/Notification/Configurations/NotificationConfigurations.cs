using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Notification
{
    public class ThongBaoConfiguration : IEntityTypeConfiguration<ThongBao>
    {
        public void Configure(EntityTypeBuilder<ThongBao> builder)
        {
            builder.ToTable("THONGBAO");

            builder.HasKey(x => x.Id).HasName("PK_THONGBAO");
            builder.Property(x => x.Id).HasColumnName("MaThongBao").ValueGeneratedOnAdd();
            builder.Property(x => x.TieuDe).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            builder.Property(x => x.NoiDung).HasColumnName("NoiDung").IsRequired();
            builder.Property(x => x.LoaiThongBao).HasColumnName("LoaiThongBao").HasMaxLength(50);
            builder.Property(x => x.NgayTao).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.NguoiTaoId).HasColumnName("NguoiTaoId");

            builder.Property(x => x.MucDo)
                .HasColumnName("MucDo")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTaoRecord").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
        }
    }

    public class NguoiNhanThongBaoConfiguration : IEntityTypeConfiguration<NguoiNhanThongBao>
    {
        public void Configure(EntityTypeBuilder<NguoiNhanThongBao> builder)
        {
            builder.ToTable("NGUOINHAN_THONGBAO");

            builder.HasKey(x => x.Id).HasName("PK_NGUOINHAN_THONGBAO");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaThongBao).HasColumnName("MaThongBao").IsRequired();
            builder.Property(x => x.MaTaiKhoan).HasColumnName("MaTaiKhoan").IsRequired();
            builder.Property(x => x.DaDoc).HasColumnName("DaDoc").HasDefaultValue(false);
            builder.Property(x => x.NgayDoc).HasColumnName("NgayDoc");

            builder.HasOne(x => x.ThongBao)
                .WithMany(x => x.NguoiNhans)
                .HasForeignKey(x => x.MaThongBao)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NGUOINHANTHONGBAO_THONGBAO");

            builder.HasIndex(x => new { x.MaThongBao, x.MaTaiKhoan })
                .IsUnique()
                .HasDatabaseName("UQ_NGUOINHANTHONGBAO_ThongBao_TaiKhoan");

            builder.HasIndex(x => x.MaTaiKhoan).HasDatabaseName("IX_NGUOINHANTHONGBAO_MaTaiKhoan");
        }
    }
}
