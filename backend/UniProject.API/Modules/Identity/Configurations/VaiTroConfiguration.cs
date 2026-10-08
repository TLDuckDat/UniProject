using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Identity
{
    public class VaiTroConfiguration : IEntityTypeConfiguration<VaiTro>
    {
        public void Configure(EntityTypeBuilder<VaiTro> builder)
        {
            builder.ToTable("VAITRO");

            builder.HasKey(x => x.Id)
                .HasName("PK_VAITRO");

            builder.Property(x => x.Id)
                .HasColumnName("MaVaiTro")
                .ValueGeneratedNever();

            builder.Property(x => x.TenVaiTro)
                .HasColumnName("TenVaiTro")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.MoTa)
                .HasColumnName("MoTa")
                .HasMaxLength(255);

            builder.HasIndex(x => x.TenVaiTro)
                .IsUnique()
                .HasDatabaseName("UQ_VAITRO_TenVaiTro");

            builder.HasData(
                new VaiTro(1, AppRoles.Admin, "Quản trị hệ thống"),
                new VaiTro(2, AppRoles.GiangVien, "Giảng viên hướng dẫn / phản biện / hội đồng"),
                new VaiTro(3, AppRoles.SinhVien, "Sinh viên thực hiện đồ án")
            );
        }
    }
}
