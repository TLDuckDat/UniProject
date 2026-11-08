// ERD-02 · Cấu hình & dữ liệu đồng bộ.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;
using Upm.Infrastructure.Persistence.Seed;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> b)
    {
        b.ToUpmTable("RuleSet");
        b.Property(x => x.MaRuleSet).Str(20);
        b.Property(x => x.TenRuleSet).Str(150);
        b.Property(x => x.CanCuPhapLy).Str(300);
        b.Property(x => x.GpaBaoVeMin).HasPrecision(3, 2);
        b.Property(x => x.GpaTotNghiepMin).HasPrecision(3, 2);
        b.Property(x => x.DiemDatDATN).HasPrecision(3, 1);
        b.Property(x => x.TrongSoQT).HasPrecision(3, 2);
        b.Property(x => x.HeSoThoiGianToiDa).HasPrecision(3, 1);
        b.Property(x => x.NguongHaHangHocLaiPct).HasPrecision(4, 1);
        b.HasIndex(x => x.MaRuleSet).IsUnique();
        b.HasData(ReferenceData.RuleSet);
    }
}

internal sealed class RuleSetKhoaHocConfiguration : IEntityTypeConfiguration<RuleSetKhoaHoc>
{
    public void Configure(EntityTypeBuilder<RuleSetKhoaHoc> b)
    {
        b.ToUpmTable("RuleSetKhoaHoc");
        b.HasFk(x => x.RuleSet, x => x.RuleSetId);
        b.HasIndex(x => new { x.KhoaHocTu, x.KhoaHocDen });
        b.HasData(ReferenceData.RuleSetKhoaHoc);
    }
}

internal sealed class ThamSoBoMonConfiguration : IEntityTypeConfiguration<ThamSoBoMon>
{
    public void Configure(EntityTypeBuilder<ThamSoBoMon> b)
    {
        b.ToUpmTable("ThamSoBoMon");
        b.Property(x => x.TrongSoQT).HasPrecision(3, 2);
        b.Property(x => x.TrongSoHD).HasPrecision(3, 2);
        b.HasFk(x => x.BoMon, x => x.BoMonId);
        b.HasFk(x => x.RuleSet, x => x.RuleSetId);
        // Một bộ tham số cho mỗi cặp (Bộ môn × RuleSet).
        b.HasIndex(x => new { x.BoMonId, x.RuleSetId }).IsUnique();
    }
}

internal sealed class ThamSoDaoVanConfiguration : IEntityTypeConfiguration<ThamSoDaoVan>
{
    public void Configure(EntityTypeBuilder<ThamSoDaoVan> b)
    {
        b.ToUpmTable("ThamSoDaoVan");
        b.Property(x => x.NguongPhanTram).HasPrecision(4, 1);
        b.HasFk(x => x.Khoa, x => x.KhoaId);
        b.HasFk(x => x.RuleSet, x => x.RuleSetId);
        b.HasIndex(x => new { x.KhoaId, x.ChuongTrinh });
    }
}

internal sealed class HocPhanConfiguration : IEntityTypeConfiguration<HocPhan>
{
    public void Configure(EntityTypeBuilder<HocPhan> b)
    {
        b.ToUpmTable("HocPhan");
        b.Property(x => x.MaHP).Str(20);
        b.Property(x => x.TenHP).Str(200);
        b.HasIndex(x => x.MaHP).IsUnique();
    }
}

internal sealed class CTDTHocPhanConfiguration : IEntityTypeConfiguration<CTDTHocPhan>
{
    public void Configure(EntityTypeBuilder<CTDTHocPhan> b)
    {
        b.ToUpmTable("CTDTHocPhan");
        b.HasFk(x => x.CTDT, x => x.CTDTId);
        b.HasFk(x => x.HocPhan, x => x.HocPhanId);
        b.HasIndex(x => new { x.CTDTId, x.HocPhanId }).IsUnique();
    }
}

internal sealed class ThongTinTienDoSVConfiguration : IEntityTypeConfiguration<ThongTinTienDoSV>
{
    public void Configure(EntityTypeBuilder<ThongTinTienDoSV> b)
    {
        b.ToUpmTable("ThongTinTienDoSV");
        b.Property(x => x.KyCuoiKeHoach).Str(10);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
        b.HasIndex(x => x.SinhVienId).IsUnique(); // 1 : 1
    }
}

internal sealed class BangDiemConfiguration : IEntityTypeConfiguration<BangDiem>
{
    public void Configure(EntityTypeBuilder<BangDiem> b)
    {
        b.ToUpmTable("BangDiem");
        b.Property(x => x.HocKy).Str(10);
        b.Property(x => x.Diem10).HasPrecision(3, 1);
        b.Property(x => x.DiemChu).Str(2);
        b.Property(x => x.Diem4).HasPrecision(2, 1);
        b.Property(x => x.NguonDongBo).Str(30);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
        b.HasFk(x => x.HocPhan, x => x.HocPhanId);
        b.HasIndex(x => new { x.SinhVienId, x.HocPhanId, x.HocKy, x.LanHoc }).IsUnique();
    }
}

internal sealed class KyLuatSVConfiguration : IEntityTypeConfiguration<KyLuatSV>
{
    public void Configure(EntityTypeBuilder<KyLuatSV> b)
    {
        b.ToUpmTable("KyLuatSV");
        b.Property(x => x.MucKyLuat).Str(50);
        b.Property(x => x.NguonCTSV).Str(30);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
    }
}
