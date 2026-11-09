// ERD-04 · Đề tài, phân công, tiến độ.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class MocTienDoConfiguration : IEntityTypeConfiguration<MocTienDo>
{
    public void Configure(EntityTypeBuilder<MocTienDo> b)
    {
        b.ToUpmTable("MocTienDo");
        b.Property(x => x.YeuCauToiThieuPct).HasPrecision(5, 2);
        b.Property(x => x.MoTa).Str(300);
        b.HasFk(x => x.Dot, x => x.DotId);
        b.HasIndex(x => new { x.DotId, x.MaMoc }).IsUnique();
    }
}

internal sealed class DeTaiConfiguration : IEntityTypeConfiguration<DeTai>
{
    public void Configure(EntityTypeBuilder<DeTai> b)
    {
        b.ToUpmTable("DeTai");
        b.Property(x => x.TenDeTai).Str(500);
        b.Property(x => x.MoTa).StrMax();
        b.HasFk(x => x.Dot, x => x.DotId);
        b.HasFk(x => x.BoMon, x => x.BoMonId);
        b.HasFk(x => x.GVDeXuat, x => x.GVDeXuatId);
        b.HasFk(x => x.SVDeXuat, x => x.SVDeXuatId);
        b.HasFk(x => x.NguoiDuyet, x => x.NguoiDuyetId);
        b.HasIndex(x => new { x.DotId, x.TrangThaiDuyet });
    }
}

internal sealed class BaoCaoTienDoConfiguration : IEntityTypeConfiguration<BaoCaoTienDo>
{
    public void Configure(EntityTypeBuilder<BaoCaoTienDo> b)
    {
        b.ToUpmTable("BaoCaoTienDo");
        b.Property(x => x.TyLeHoanThanhPct).HasPrecision(5, 2);
        b.Property(x => x.GVHDNhanXet).Str(1000);
        b.Property(x => x.DiemTienDo).HasPrecision(3, 1);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.MocTienDo, x => x.MocTienDoId);
    }
}

internal sealed class DangKyNguyenVongConfiguration : IEntityTypeConfiguration<DangKyNguyenVong>
{
    public void Configure(EntityTypeBuilder<DangKyNguyenVong> b)
    {
        b.ToUpmTable("DangKyNguyenVong");
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.DeTai, x => x.DeTaiId);
        b.HasIndex(x => new { x.SVTrongDotId, x.ThuTuNguyenVong }).IsUnique();
        b.HasIndex(x => new { x.SVTrongDotId, x.DeTaiId }).IsUnique();
    }
}

internal sealed class KhaiBaoXungDotConfiguration : IEntityTypeConfiguration<KhaiBaoXungDot>
{
    public void Configure(EntityTypeBuilder<KhaiBaoXungDot> b)
    {
        b.ToUpmTable("KhaiBaoXungDot");
        b.Property(x => x.LyDo).Str(300);
        b.HasFk(x => x.GiangVien, x => x.GiangVienId);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
        b.HasIndex(x => new { x.GiangVienId, x.SinhVienId }).IsUnique();
    }
}

internal sealed class PhanCongConfiguration : IEntityTypeConfiguration<PhanCong>
{
    public void Configure(EntityTypeBuilder<PhanCong> b)
    {
        b.ToUpmTable("PhanCong");
        b.Property(x => x.LyDoThayDoi).Str(300);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId, s => s.PhanCongs);
        b.HasFk(x => x.GiangVien, x => x.GiangVienId);
        b.HasFk(x => x.QuyetDinh, x => x.QuyetDinhId);
        b.HasFk(x => x.VuotQuotaDuyetBoi, x => x.VuotQuotaDuyetBoiId);
        b.HasIndex(x => new { x.SVTrongDotId, x.VaiTro });
    }
}

internal sealed class PhieuGiaoNhiemVuConfiguration : IEntityTypeConfiguration<PhieuGiaoNhiemVu>
{
    public void Configure(EntityTypeBuilder<PhieuGiaoNhiemVu> b)
    {
        b.ToUpmTable("PhieuGiaoNhiemVu");
        b.Property(x => x.NoiDungNhiemVu).StrMax();
        b.Property(x => x.YeuCauSanPham).StrMax();
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
    }
}
