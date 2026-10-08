using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniProject.API.Modules.Project
{
    public class DoAnConfiguration : IEntityTypeConfiguration<DoAn>
    {
        public void Configure(EntityTypeBuilder<DoAn> builder)
        {
            builder.ToTable("DOAN");

            builder.HasKey(x => x.Id).HasName("PK_DOAN");
            builder.Property(x => x.Id).HasColumnName("MaDoAn").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAnCode).HasColumnName("MaDoAnCode").HasMaxLength(20).IsRequired();
            builder.Property(x => x.TenDoAn).HasColumnName("TenDoAn").HasMaxLength(250).IsRequired();
            builder.Property(x => x.MaDotDoAn).HasColumnName("MaDotDoAn").IsRequired();
            builder.Property(x => x.MaDeTai).HasColumnName("MaDeTai").IsRequired();
            builder.Property(x => x.MaDangKy).HasColumnName("MaDangKy");
            builder.Property(x => x.NgayTao).HasColumnName("NgayTao").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.NgayBatDau).HasColumnName("NgayBatDau");
            builder.Property(x => x.NgayKetThuc).HasColumnName("NgayKetThuc");

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("NgayTaoRecord").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

            builder.HasIndex(x => x.MaDoAnCode).IsUnique().HasDatabaseName("UQ_DOAN_MaDoAnCode");
            builder.HasIndex(x => x.MaDotDoAn).HasDatabaseName("IX_DOAN_MaDotDoAn");
            builder.HasIndex(x => x.MaDeTai).HasDatabaseName("IX_DOAN_MaDeTai");
        }
    }

    public class ThanhVienDoAnConfiguration : IEntityTypeConfiguration<ThanhVienDoAn>
    {
        public void Configure(EntityTypeBuilder<ThanhVienDoAn> builder)
        {
            builder.ToTable("THANHVIEN_DOAN");

            builder.HasKey(x => x.Id).HasName("PK_THANHVIEN_DOAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaSV).HasColumnName("MaSV").IsRequired();

            builder.Property(x => x.VaiTro)
                .HasColumnName("VaiTro")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DoAn)
                .WithMany(x => x.ThanhViens)
                .HasForeignKey(x => x.MaDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_THANHVIENDOAN_DOAN");

            builder.HasIndex(x => new { x.MaDoAn, x.MaSV })
                .IsUnique()
                .HasDatabaseName("UQ_THANHVIENDOAN_MaDoAn_MaSV");

            // Index unique có điều kiện: mỗi đồ án chỉ 1 trưởng nhóm
            builder.HasIndex(x => x.MaDoAn)
                .IsUnique()
                .HasFilter("[VaiTro] = 'TRUONG_NHOM'")
                .HasDatabaseName("UQ_THANHVIENDOAN_OneLeader");
        }
    }

    public class PhanCongHuongDanConfiguration : IEntityTypeConfiguration<PhanCongHuongDan>
    {
        public void Configure(EntityTypeBuilder<PhanCongHuongDan> builder)
        {
            builder.ToTable("PHANCONG_HUONGDAN");

            builder.HasKey(x => x.Id).HasName("PK_PHANCONG_HUONGDAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaGV).HasColumnName("MaGV").IsRequired();
            builder.Property(x => x.NgayPhanCong).HasColumnName("NgayPhanCong").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.TrangThai).HasColumnName("TrangThai").HasDefaultValue(true);

            builder.Property(x => x.LoaiHuongDan)
                .HasColumnName("LoaiHuongDan")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DoAn)
                .WithMany(x => x.PhanCongHuongDans)
                .HasForeignKey(x => x.MaDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PHANCONGHUONGDAN_DOAN");

            // Index unique có điều kiện: mỗi đồ án chỉ 1 GV hướng dẫn chính đang hoạt động
            builder.HasIndex(x => x.MaDoAn)
                .IsUnique()
                .HasFilter("[LoaiHuongDan] = 'CHINH' AND [TrangThai] = 1")
                .HasDatabaseName("UQ_PHANCONGHUONGDAN_OnePrimaryAdvisor");

            builder.HasIndex(x => x.MaGV).HasDatabaseName("IX_PHANCONG_HUONGDAN_MaGV");
        }
    }

    public class BaoCaoTienDoConfiguration : IEntityTypeConfiguration<BaoCaoTienDo>
    {
        public void Configure(EntityTypeBuilder<BaoCaoTienDo> builder)
        {
            builder.ToTable("BAOCAO_TIENDO", table =>
            {
                table.HasCheckConstraint("CK_BAOCAOTIENDO_PhanTram", "[PhanTramHoanThanh] BETWEEN 0 AND 100");
            });

            builder.HasKey(x => x.Id).HasName("PK_BAOCAO_TIENDO");
            builder.Property(x => x.Id).HasColumnName("MaBaoCao").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaSVNop).HasColumnName("MaSVNop").IsRequired();
            builder.Property(x => x.LanBaoCao).HasColumnName("LanBaoCao").IsRequired();
            builder.Property(x => x.TieuDe).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            builder.Property(x => x.NoiDung).HasColumnName("NoiDung").IsRequired();
            builder.Property(x => x.CongViecDaLam).HasColumnName("CongViecDaLam");
            builder.Property(x => x.CongViecTiepTheo).HasColumnName("CongViecTiepTheo");
            builder.Property(x => x.KhoKhan).HasColumnName("KhoKhan");
            builder.Property(x => x.PhanTramHoanThanh).HasColumnName("PhanTramHoanThanh").IsRequired();
            builder.Property(x => x.NgayNop).HasColumnName("NgayNop").HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.DoAn)
                .WithMany(x => x.BaoCaoTienDos)
                .HasForeignKey(x => x.MaDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BAOCAOTIENDO_DOAN");

            builder.HasIndex(x => x.MaDoAn).HasDatabaseName("IX_BAOCAOTIENDO_MaDoAn");
        }
    }

    public class NhanXetTienDoConfiguration : IEntityTypeConfiguration<NhanXetTienDo>
    {
        public void Configure(EntityTypeBuilder<NhanXetTienDo> builder)
        {
            builder.ToTable("NHANXET_TIENDO");

            builder.HasKey(x => x.Id).HasName("PK_NHANXET_TIENDO");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaBaoCao).HasColumnName("MaBaoCao").IsRequired();
            builder.Property(x => x.MaGV).HasColumnName("MaGV").IsRequired();
            builder.Property(x => x.NoiDungNhanXet).HasColumnName("NoiDungNhanXet").IsRequired();
            builder.Property(x => x.NgayNhanXet).HasColumnName("NgayNhanXet").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.MucDanhGia)
                .HasColumnName("MucDanhGia")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.BaoCaoTienDo)
                .WithMany(x => x.NhanXetTienDos)
                .HasForeignKey(x => x.MaBaoCao)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NHANXETTIENDO_BAOCAOTIENDO");
        }
    }

    public class YeuCauChinhSuaConfiguration : IEntityTypeConfiguration<YeuCauChinhSua>
    {
        public void Configure(EntityTypeBuilder<YeuCauChinhSua> builder)
        {
            builder.ToTable("YEUCAU_CHINHSUA");

            builder.HasKey(x => x.Id).HasName("PK_YEUCAU_CHINHSUA");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaGVYeuCau).HasColumnName("MaGVYeuCau").IsRequired();
            builder.Property(x => x.TieuDe).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            builder.Property(x => x.NoiDung).HasColumnName("NoiDung").IsRequired();
            builder.Property(x => x.HanHoanThanh).HasColumnName("HanHoanThanh");
            builder.Property(x => x.NgayYeuCau).HasColumnName("NgayYeuCau").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.TrangThai)
                .HasColumnName("TrangThai")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DoAn)
                .WithMany(x => x.YeuCauChinhSuas)
                .HasForeignKey(x => x.MaDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_YEUCAUCHINHSUA_DOAN");
        }
    }

    public class TaiLieuDoAnConfiguration : IEntityTypeConfiguration<TaiLieuDoAn>
    {
        public void Configure(EntityTypeBuilder<TaiLieuDoAn> builder)
        {
            builder.ToTable("TAILIEU_DOAN");

            builder.HasKey(x => x.Id).HasName("PK_TAILIEU_DOAN");
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(x => x.MaDoAn).HasColumnName("MaDoAn").IsRequired();
            builder.Property(x => x.MaSVNop).HasColumnName("MaSVNop").IsRequired();
            builder.Property(x => x.TenTaiLieu).HasColumnName("TenTaiLieu").HasMaxLength(250).IsRequired();
            builder.Property(x => x.DuongDanFile).HasColumnName("DuongDanFile").HasMaxLength(500).IsRequired();
            builder.Property(x => x.Version).HasColumnName("Version").HasDefaultValue(1);
            builder.Property(x => x.NgayUpload).HasColumnName("NgayUpload").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.LoaiTaiLieu)
                .HasColumnName("LoaiTaiLieu")
                .HasMaxLength(30)
                .HasConversion<string>();

            builder.HasOne(x => x.DoAn)
                .WithMany(x => x.TaiLieuDoAns)
                .HasForeignKey(x => x.MaDoAn)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TAILIEUDOAN_DOAN");
        }
    }
}
