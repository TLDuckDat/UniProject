// ERD-03 · Đợt & xét điều kiện giao.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class DotDATNConfiguration : IEntityTypeConfiguration<DotDATN>
{
    public void Configure(EntityTypeBuilder<DotDATN> b)
    {
        b.ToUpmTable("DotDATN");
        b.Property(x => x.MaDot).Str(20);
        b.Property(x => x.TenDot).Str(150);
        b.Property(x => x.NamHoc).Str(9);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => x.MaDot).IsUnique();
        b.HasIndex(x => new { x.CoSoId, x.TrangThai });
    }
}

internal sealed class SVTrongDotConfiguration : IEntityTypeConfiguration<SVTrongDot>
{
    public void Configure(EntityTypeBuilder<SVTrongDot> b)
    {
        b.ToUpmTable("SVTrongDot");
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasFk(x => x.Dot, x => x.DotId);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
        b.HasFk(x => x.CTDT, x => x.CTDTId);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasFk(x => x.DeTai, x => x.DeTaiId, d => d.SVTrongDots);
        b.HasFk(x => x.QuyetDinhGiao, x => x.QuyetDinhGiaoId);
        b.HasFk(x => x.HoSoTruoc, x => x.HoSoTruocId);
        // Học 2 CTĐT = 2 hồ sơ trong cùng đợt.
        b.HasIndex(x => new { x.DotId, x.SinhVienId, x.CTDTId }).IsUnique();
        b.HasIndex(x => new { x.DotId, x.TrangThai });
        b.HasIndex(x => new { x.CoSoId, x.TrangThai });
    }
}

internal sealed class XetDieuKienConfiguration : IEntityTypeConfiguration<XetDieuKien>
{
    public void Configure(EntityTypeBuilder<XetDieuKien> b)
    {
        b.ToUpmTable("XetDieuKien");
        b.Property(x => x.RuleVersion).Str(20);
        b.Property(x => x.KetQua).Str(40);
        b.Property(x => x.GpaTichLuy).HasPrecision(3, 2);
        b.Property(x => x.LyDo).Str(500);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.RuleSet, x => x.RuleSetId);
        b.HasFk(x => x.NguoiChay, x => x.NguoiChayId);
        b.HasIndex(x => new { x.SVTrongDotId, x.LoaiXet, x.ThoiDiemXet });
    }
}

internal sealed class XetDieuKienChiTietConfiguration : IEntityTypeConfiguration<XetDieuKienChiTiet>
{
    public void Configure(EntityTypeBuilder<XetDieuKienChiTiet> b)
    {
        b.ToUpmTable("XetDieuKienChiTiet");
        b.HasFk(x => x.XetDieuKien, x => x.XetDieuKienId, k => k.ChiTiets);
        b.HasFk(x => x.HocPhan, x => x.HocPhanId);
    }
}
