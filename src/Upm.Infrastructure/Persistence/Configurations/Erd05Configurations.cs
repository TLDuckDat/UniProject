// ERD-05 · Quyển, đọc duyệt, đạo văn.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class PhienBanQuyenConfiguration : IEntityTypeConfiguration<PhienBanQuyen>
{
    public void Configure(EntityTypeBuilder<PhienBanQuyen> b)
    {
        b.ToUpmTable("PhienBanQuyen");
        b.Property(x => x.HashSHA256).HasMaxLength(64).IsFixedLength().IsUnicode(false); // char(64)
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasIndex(x => new { x.SVTrongDotId, x.SoPhienBan }).IsUnique();
        b.HasIndex(x => x.HashSHA256);
    }
}

internal sealed class YKienGVHDConfiguration : IEntityTypeConfiguration<YKienGVHD>
{
    public void Configure(EntityTypeBuilder<YKienGVHD> b)
    {
        b.ToUpmTable("YKienGVHD");
        b.Property(x => x.LyDoKhongDongY).Str(1000);
        b.Property(x => x.DiemGVHD).HasPrecision(3, 1);
        b.Property(x => x.DiemKhac).HasPrecision(3, 1);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.GiangVien, x => x.GiangVienId);
    }
}

internal sealed class KetQuaDocDuyetConfiguration : IEntityTypeConfiguration<KetQuaDocDuyet>
{
    public void Configure(EntityTypeBuilder<KetQuaDocDuyet> b)
    {
        b.ToUpmTable("KetQuaDocDuyet");
        b.Property(x => x.NhanXet).Str(2000);
        b.Property(x => x.DiemGVDD).HasPrecision(3, 1);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.PhienBanQuyen, x => x.PhienBanQuyenId);
        b.HasFk(x => x.GVDD, x => x.GVDDId);
    }
}

internal sealed class KetQuaKiemTraDaoVanConfiguration : IEntityTypeConfiguration<KetQuaKiemTraDaoVan>
{
    public void Configure(EntityTypeBuilder<KetQuaKiemTraDaoVan> b)
    {
        b.ToUpmTable("KetQuaKiemTraDaoVan");
        b.Property(x => x.PhanMemQuet).Str(50);
        b.Property(x => x.TyLeTrungPct).HasPrecision(5, 2);
        b.Property(x => x.NguongApDungPct).HasPrecision(5, 2);
        b.HasFk(x => x.PhienBanQuyen, x => x.PhienBanQuyenId);
    }
}

internal sealed class LuuTruThuVienConfiguration : IEntityTypeConfiguration<LuuTruThuVien>
{
    public void Configure(EntityTypeBuilder<LuuTruThuVien> b)
    {
        b.ToUpmTable("LuuTruThuVien");
        b.Property(x => x.MaLuuTru).Str(50);
        b.Property(x => x.GhiChu).Str(300);
        b.HasFk(x => x.PhienBanQuyen, x => x.PhienBanQuyenId);
        b.HasFk(x => x.NguoiNhan, x => x.NguoiNhanId);
    }
}
