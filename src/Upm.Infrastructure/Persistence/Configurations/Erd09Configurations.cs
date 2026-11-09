using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class TepDinhKemConfiguration : IEntityTypeConfiguration<TepDinhKem>
{
    public void Configure(EntityTypeBuilder<TepDinhKem> b)
    {
        b.ToUpmTable("TepDinhKem");
        b.Property(x => x.TenGoc).Str(260);
        b.Property(x => x.ContentType).Str(100);
        b.Property(x => x.DuongDan).Str(400);
        b.Property(x => x.HashSHA256).Str(64);
        b.HasIndex(x => x.HashSHA256);
    }
}

internal sealed class ThongBaoConfiguration : IEntityTypeConfiguration<ThongBao>
{
    public void Configure(EntityTypeBuilder<ThongBao> b)
    {
        b.ToUpmTable("ThongBao");
        b.Property(x => x.TieuDe).Str(200);
        b.Property(x => x.NoiDung).Str(2000);
        b.HasFk(x => x.TaiKhoan, x => x.TaiKhoanId);
        b.HasIndex(x => new { x.TaiKhoanId, x.DaDoc });
    }
}
