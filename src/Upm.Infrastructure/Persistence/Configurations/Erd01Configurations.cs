// ERD-01 · Tổ chức & nhân sự.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;
using Upm.Infrastructure.Persistence.Seed;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class CoSoConfiguration : IEntityTypeConfiguration<CoSo>
{
    public void Configure(EntityTypeBuilder<CoSo> b)
    {
        b.ToUpmTable("CoSo");
        b.Property(x => x.MaCoSo).Str(10);
        b.Property(x => x.TenCoSo).Str(100);
        b.HasIndex(x => x.MaCoSo).IsUnique();
        b.HasData(ReferenceData.CoSo);
    }
}

internal sealed class KhoaConfiguration : IEntityTypeConfiguration<Khoa>
{
    public void Configure(EntityTypeBuilder<Khoa> b)
    {
        b.ToUpmTable("Khoa");
        b.Property(x => x.MaKhoa).Str(20);
        b.Property(x => x.TenKhoa).Str(150);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => new { x.CoSoId, x.MaKhoa }).IsUnique();
    }
}

internal sealed class BoMonConfiguration : IEntityTypeConfiguration<BoMon>
{
    public void Configure(EntityTypeBuilder<BoMon> b)
    {
        b.ToUpmTable("BoMon");
        b.Property(x => x.MaBM).Str(20);
        b.Property(x => x.TenBM).Str(150);
        b.HasFk(x => x.Khoa, x => x.KhoaId);
        b.HasIndex(x => new { x.KhoaId, x.MaBM }).IsUnique();
    }
}

internal sealed class GiangVienConfiguration : IEntityTypeConfiguration<GiangVien>
{
    public void Configure(EntityTypeBuilder<GiangVien> b)
    {
        b.ToUpmTable("GiangVien");
        b.Property(x => x.MaGV).Str(20);
        b.Property(x => x.HoTen).Str(100);
        b.Property(x => x.HocHam).Str(30);
        b.Property(x => x.HocVi).Str(30);
        b.Property(x => x.Email).Str(150);
        b.HasFk(x => x.BoMon, x => x.BoMonId);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => x.MaGV).IsUnique();
    }
}

internal sealed class SinhVienConfiguration : IEntityTypeConfiguration<SinhVien>
{
    public void Configure(EntityTypeBuilder<SinhVien> b)
    {
        b.ToUpmTable("SinhVien");
        b.Property(x => x.MaSV).Str(20);
        b.Property(x => x.HoTen).Str(100);
        b.Property(x => x.Email).Str(150);
        b.Property(x => x.NguonDongBo).Str(30);
        b.HasFk(x => x.CTDT, x => x.CTDTId);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => x.MaSV).IsUnique();
        b.HasIndex(x => x.KhoaHoc);
    }
}

internal sealed class ChuongTrinhDaoTaoConfiguration : IEntityTypeConfiguration<ChuongTrinhDaoTao>
{
    public void Configure(EntityTypeBuilder<ChuongTrinhDaoTao> b)
    {
        b.ToUpmTable("ChuongTrinhDaoTao");
        b.Property(x => x.MaCTDT).Str(20);
        b.Property(x => x.TenCTDT).Str(200);
        b.Property(x => x.Nganh).Str(150);
        b.HasFk(x => x.Khoa, x => x.KhoaId);
        b.HasIndex(x => x.MaCTDT).IsUnique();
    }
}

internal sealed class TaiKhoanConfiguration : IEntityTypeConfiguration<TaiKhoan>
{
    public void Configure(EntityTypeBuilder<TaiKhoan> b)
    {
        b.ToUpmTable("TaiKhoan");
        b.Property(x => x.TenDangNhap).Str(50);
        b.Property(x => x.Email).Str(150);
        b.Property(x => x.MatKhauHash).Str(200); // PasswordHasher V3 ~ 84 ký tự base64
        b.HasFk(x => x.GiangVien, x => x.GiangVienId);
        b.HasFk(x => x.SinhVien, x => x.SinhVienId);
        b.HasIndex(x => x.TenDangNhap).IsUnique();
    }
}

internal sealed class VaiTroConfiguration : IEntityTypeConfiguration<VaiTro>
{
    public void Configure(EntityTypeBuilder<VaiTro> b)
    {
        b.ToUpmTable("VaiTro");
        b.Property(x => x.MaVaiTro).Str(30);
        b.Property(x => x.TenVaiTro).Str(100);
        b.Property(x => x.MoTa).Str(300);
        b.HasIndex(x => x.MaVaiTro).IsUnique();
        b.HasData(ReferenceData.VaiTro);
    }
}

internal sealed class TaiKhoanVaiTroConfiguration : IEntityTypeConfiguration<TaiKhoanVaiTro>
{
    public void Configure(EntityTypeBuilder<TaiKhoanVaiTro> b)
    {
        b.ToUpmTable("TaiKhoanVaiTro");
        b.HasFk(x => x.TaiKhoan, x => x.TaiKhoanId);
        b.HasFk(x => x.VaiTro, x => x.VaiTroId);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasFk(x => x.Khoa, x => x.KhoaId);
        b.HasFk(x => x.BoMon, x => x.BoMonId);
    }
}
