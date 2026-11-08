// [Bổ sung Phần 3] Bảng xác thực.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToUpmTable("RefreshToken");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        b.Property(x => x.LyDoThuHoi).Str(50);
        b.Property(x => x.TaoTuIp).Str(45);
        b.HasFk(x => x.TaiKhoan, x => x.TaiKhoanId);
        b.HasFk(x => x.ThayTheBoi, x => x.ThayTheBoiId);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.HoToken);
    }
}
