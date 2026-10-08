using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Registration
{
    public class DangKyDeTaiConfiguration : IEntityTypeConfiguration<DangKyDeTai>
    {
        public void Configure(EntityTypeBuilder<DangKyDeTai> builder)
        {
            builder.ToTable("DANGKYDETAI");

            builder.HasKey(x => x.Id).HasName("PK_DANGKYDETAI");
            builder.Property(x => x.Id).HasColumnName("MaDangKy").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();
            builder.Property(x => x.TenNhom).HasColumnName("TenNhom").HasMaxLength(100);
            builder.Property(x => x.NgayDangKy).HasColumnName("NgayDangKy").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

            builder.HasIndex(x => x.MaDotDoAn).HasDatabaseName("IX_DANGKYDETAI_MaDotDoAn");
        }
    }

    public class ThanhVienDangKyConfiguration : IEntityTypeConfiguration<ThanhVienDangKy>
    {
        public void Configure(EntityTypeBuilder<ThanhVienDangKy> builder)
        {
            builder.ToTable("THANHVIEN_DANGKY");

            builder.HasKey(x => x.Id).HasName("PK_THANHVIEN_DANGKY");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDangKy).HasColumnName("MaDangKy").IsRequired();
            builder.Property(x => x.MaSV).HasColumnName("MaSV").IsRequired();
            builder.Property(x => x.NgayThamGia).HasColumnName("NgayThamGia").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.VaiTro)
                .HasColumnName("VaiTro")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DangKyDeTai)
                .WithMany(x => x.ThanhViens)
                .HasForeignKey(x => x.MaDangKy)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_THANHVIENDANGKY_DANGKYDETAI");

            builder.HasIndex(x => new { x.MaDangKy, x.MaSV })
                .IsUnique()
                .HasDatabaseName("UQ_THANHVIENDANGKY_MaDangKy_MaSV");

            // Index unique có điều kiện: mỗi đơn chỉ 1 trưởng nhóm
            builder.HasIndex(x => x.MaDangKy)
                .IsUnique()
                .HasFilter("[VaiTro] = 'TRUONG_NHOM'")
                .HasDatabaseName("UQ_THANHVIENDANGKY_OneLeader");
        }
    }

    public class YeuCauThayDoiConfiguration : IEntityTypeConfiguration<YeuCauThayDoi>
    {
        public void Configure(EntityTypeBuilder<YeuCauThayDoi> builder)
        {
            builder.ToTable("YEUCAU_THAYDOI", table =>
            {
                // Đổi đề tài thì có đề tài mới, đổi nhóm thì có nhóm mới; không có cả hai.
                table.HasCheckConstraint("CK_YEUCAUTHAYDOI_Loai",
                    "([LoaiYeuCau] = 'DOI_DE_TAI' AND [MaDeTaiDotDoAnMoi] IS NOT NULL AND [MaDangKyMoi] IS NULL) OR " +
                    "([LoaiYeuCau] = 'DOI_NHOM' AND [MaDangKyMoi] IS NOT NULL AND [MaDeTaiDotDoAnMoi] IS NULL)");
            });

            builder.HasKey(x => x.Id).HasName("PK_YEUCAU_THAYDOI");
            builder.Property(x => x.Id).HasColumnName("MaYeuCau").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDangKy).HasColumnName("MaDangKy").IsRequired();
            builder.Property(x => x.MaSVYeuCau).HasColumnName("MaSVYeuCau").IsRequired();
            builder.Property(x => x.LoaiYeuCau).HasColumnName("LoaiYeuCau").HasMaxLength(30).HasConversion<string>().IsRequired();
            builder.Property(x => x.MaDeTaiDotDoAnMoi).HasColumnName("MaDeTaiDotDoAnMoi");
            builder.Property(x => x.MaDangKyMoi).HasColumnName("MaDangKyMoi");
            builder.Property(x => x.LyDo).HasColumnName("LyDo").HasMaxLength(1000).IsRequired();
            builder.Property(x => x.TrangThai).HasColumnName("TrangThai").HasMaxLength(30).HasConversion<string>();
            builder.Property(x => x.NgayYeuCau).HasColumnName("NgayYeuCau").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.MaTaiKhoanXuLy).HasColumnName("MaTaiKhoanXuLy");
            builder.Property(x => x.NgayXuLy).HasColumnName("NgayXuLy");
            builder.Property(x => x.GhiChuXuLy).HasColumnName("GhiChuXuLy").HasMaxLength(500);
            builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne<DangKyDeTai>().WithMany().HasForeignKey(x => x.MaDangKy)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_YEUCAUTHAYDOI_DANGKYDETAI");
            builder.HasOne<DangKyDeTai>().WithMany().HasForeignKey(x => x.MaDangKyMoi)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_YEUCAUTHAYDOI_DANGKYDETAI_Moi");

            builder.HasIndex(x => x.MaDangKy).HasDatabaseName("IX_YEUCAUTHAYDOI_MaDangKy");
            builder.HasIndex(x => new { x.MaSVYeuCau, x.TrangThai }).HasDatabaseName("IX_YEUCAUTHAYDOI_MaSV_TrangThai");
        }
    }

    public class ChiTietDangKyConfiguration : IEntityTypeConfiguration<ChiTietDangKy>
    {
        public void Configure(EntityTypeBuilder<ChiTietDangKy> builder)
        {
            builder.ToTable("CHITIET_DANGKY", table =>
            {
                table.HasCheckConstraint("CK_CHITIETDANGKY_ThuTu", "[ThuTuUuTien] BETWEEN 1 AND 10");
            });

            builder.HasKey(x => x.Id).HasName("PK_CHITIET_DANGKY");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDangKy).HasColumnName("MaDangKy").IsRequired();
            builder.Property(x => x.MaDeTaiDotDoAn).HasColumnName("MaDeTaiDotDoAn").IsRequired();
            builder.Property(x => x.ThuTuUuTien).HasColumnName("ThuTuUuTien").IsRequired();
            builder.Property(x => x.MaGVDuyet).HasColumnName("MaGVDuyet");
            builder.Property(x => x.NgayDuyet).HasColumnName("NgayDuyet");
            builder.Property(x => x.LyDoTuChoi).HasColumnName("LyDoTuChoi").HasMaxLength(500);

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DangKyDeTai)
                .WithMany(x => x.ChiTietDangKys)
                .HasForeignKey(x => x.MaDangKy)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CHITIETDANGKY_DANGKYDETAI");

            builder.HasIndex(x => new { x.MaDangKy, x.MaDeTaiDotDoAn })
                .IsUnique()
                .HasDatabaseName("UQ_CHITIETDANGKY_MaDangKy_MaDeTaiDotDoAn");

            builder.HasIndex(x => new { x.MaDangKy, x.ThuTuUuTien })
                .IsUnique()
                .HasDatabaseName("UQ_CHITIETDANGKY_MaDangKy_ThuTuUuTien");

            // Index unique có điều kiện: mỗi đơn tối đa 1 nguyện vọng DUOC_CHON
            builder.HasIndex(x => x.MaDangKy)
                .IsUnique()
                .HasFilter("[TrangThai] = 'DUOC_CHON'")
                .HasDatabaseName("UQ_CHITIETDANGKY_OneApprovedPreference");
        }
    }
}
