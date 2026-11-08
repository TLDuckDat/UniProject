// ERD-06 · Hội đồng, bảo vệ, điểm.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class HoiDongConfiguration : IEntityTypeConfiguration<HoiDong>
{
    public void Configure(EntityTypeBuilder<HoiDong> b)
    {
        b.ToUpmTable("HoiDong");
        b.Property(x => x.MaHD).Str(20);
        b.Property(x => x.TenHD).Str(200);
        b.Property(x => x.LinkGhiHinh).Str(500);
        b.HasFk(x => x.Dot, x => x.DotId);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasFk(x => x.Khoa, x => x.KhoaId);
        b.HasFk(x => x.QuyetDinh, x => x.QuyetDinhId);
        b.HasIndex(x => new { x.DotId, x.MaHD }).IsUnique();
        b.HasIndex(x => new { x.CoSoId, x.TrangThai });
    }
}

internal sealed class ThanhVienHoiDongConfiguration : IEntityTypeConfiguration<ThanhVienHoiDong>
{
    public void Configure(EntityTypeBuilder<ThanhVienHoiDong> b)
    {
        b.ToUpmTable("ThanhVienHoiDong");
        b.Property(x => x.LyDoThayThe).Str(300);
        b.HasFk(x => x.HoiDong, x => x.HoiDongId, h => h.ThanhViens);
        b.HasFk(x => x.GiangVien, x => x.GiangVienId);
        b.HasFk(x => x.ThayTheCho, x => x.ThayTheChoId);
    }
}

internal sealed class PhongBaoVeConfiguration : IEntityTypeConfiguration<PhongBaoVe>
{
    public void Configure(EntityTypeBuilder<PhongBaoVe> b)
    {
        b.ToUpmTable("PhongBaoVe");
        b.Property(x => x.TenPhong).Str(50);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => new { x.CoSoId, x.TenPhong }).IsUnique();
    }
}

internal sealed class LichBaoVeConfiguration : IEntityTypeConfiguration<LichBaoVe>
{
    public void Configure(EntityTypeBuilder<LichBaoVe> b)
    {
        b.ToUpmTable("LichBaoVe");
        b.Property(x => x.TruDiemViPhamPct).HasPrecision(4, 1);
        b.Property(x => x.GhiChu).Str(300);
        b.HasFk(x => x.HoiDong, x => x.HoiDongId);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.PhongBaoVe, x => x.PhongBaoVeId);
        // Kiểm tra trùng phòng/ca khi xếp lịch.
        b.HasIndex(x => new { x.PhongBaoVeId, x.NgayBaoVe, x.Ca });
    }
}

internal sealed class PhieuDiemConfiguration : IEntityTypeConfiguration<PhieuDiem>
{
    public void Configure(EntityTypeBuilder<PhieuDiem> b)
    {
        b.ToUpmTable("PhieuDiem");
        b.Property(x => x.Diem).HasPrecision(3, 1);
        b.Property(x => x.NhanXet).Str(1000);
        b.HasFk(x => x.LichBaoVe, x => x.LichBaoVeId, l => l.PhieuDiems);
        b.HasFk(x => x.ThanhVienHoiDong, x => x.ThanhVienHoiDongId);
        b.HasFk(x => x.NguoiNhap, x => x.NguoiNhapId);
        // Mỗi thành viên một phiếu cho một buổi bảo vệ.
        b.HasIndex(x => new { x.LichBaoVeId, x.ThanhVienHoiDongId }).IsUnique();
    }
}

internal sealed class BienBanConfiguration : IEntityTypeConfiguration<BienBan>
{
    public void Configure(EntityTypeBuilder<BienBan> b)
    {
        b.ToUpmTable("BienBan");
        b.Property(x => x.NoiDung).StrMax();
        b.Property(x => x.ChenhLechMax).HasPrecision(3, 1);
        b.HasFk(x => x.LichBaoVe, x => x.LichBaoVeId);
        b.HasFk(x => x.ThuKy, x => x.ThuKyId);
    }
}

internal sealed class DiemDATNConfiguration : IEntityTypeConfiguration<DiemDATN>
{
    public void Configure(EntityTypeBuilder<DiemDATN> b)
    {
        b.ToUpmTable("DiemDATN");
        b.Property(x => x.RuleVersion).Str(20);
        b.Property(x => x.DiemGVHD).HasPrecision(3, 1);
        b.Property(x => x.DiemGVDD).HasPrecision(3, 1);
        b.Property(x => x.DiemKhac).HasPrecision(3, 1);
        b.Property(x => x.DiemQuaTrinh).HasPrecision(3, 1);
        b.Property(x => x.DiemBaoVe).HasPrecision(3, 1);
        b.Property(x => x.TrongSoQT).HasPrecision(3, 2);
        b.Property(x => x.TrongSoHD).HasPrecision(3, 2);
        b.Property(x => x.DiemTong).HasColumnName("DiemDATN").HasPrecision(3, 1);
        b.Property(x => x.DiemChu).Str(2);
        b.Property(x => x.DiemHe4).HasPrecision(2, 1);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.RuleSet, x => x.RuleSetId);
        b.HasFk(x => x.ChotBoi, x => x.ChotBoiId);
        b.HasFk(x => x.QuyetDinhSuaDiem, x => x.QuyetDinhSuaDiemId);
        b.HasIndex(x => x.SVTrongDotId).IsUnique(); // 1 : 1
    }
}
