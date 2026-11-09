// ERD-07 · Quyết định, đơn từ, log.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Upm.Domain.Entities;

namespace Upm.Infrastructure.Persistence.Configurations;

internal sealed class QuyetDinhConfiguration : IEntityTypeConfiguration<QuyetDinh>
{
    public void Configure(EntityTypeBuilder<QuyetDinh> b)
    {
        b.ToUpmTable("QuyetDinh");
        b.Property(x => x.SoQuyetDinh).Str(50);
        b.Property(x => x.NguoiKy).Str(100);
        b.Property(x => x.GhiChu).Str(300);
        b.HasFk(x => x.CoSo, x => x.CoSoId);
        b.HasIndex(x => new { x.CoSoId, x.SoQuyetDinh }).IsUnique();
    }
}

internal sealed class QuyetDinhDoiTuongConfiguration : IEntityTypeConfiguration<QuyetDinhDoiTuong>
{
    public void Configure(EntityTypeBuilder<QuyetDinhDoiTuong> b)
    {
        b.ToUpmTable("QuyetDinhDoiTuong");
        b.HasFk(x => x.QuyetDinh, x => x.QuyetDinhId, q => q.DoiTuongs);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.HoiDong, x => x.HoiDongId);
        b.HasFk(x => x.PhanCong, x => x.PhanCongId);
        b.HasFk(x => x.DiemDATN, x => x.DiemDATNId);
        // Mỗi dòng chỉ trỏ đúng MỘT đối tượng (thiet-ke.md ERD-07).
        b.ToTable(t => t.HasCheckConstraint(
            "CK_QuyetDinhDoiTuong_MotDoiTuong",
            "(CASE WHEN [SVTrongDotId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [HoiDongId] IS NULL THEN 0 ELSE 1 END"
            + " + CASE WHEN [PhanCongId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [DiemDATNId] IS NULL THEN 0 ELSE 1 END) = 1"));
    }
}

internal sealed class DonTuConfiguration : IEntityTypeConfiguration<DonTu>
{
    public void Configure(EntityTypeBuilder<DonTu> b)
    {
        b.ToUpmTable("DonTu");
        b.Property(x => x.NoiDung).StrMax();
        b.Property(x => x.KetQuaXuLy).Str(1000);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        b.HasFk(x => x.NguoiXuLy, x => x.NguoiXuLyId);
        b.HasFk(x => x.QuyetDinh, x => x.QuyetDinhId);
        b.HasIndex(x => new { x.TrangThai, x.LoaiDon });
    }
}

internal sealed class LichSuTrangThaiConfiguration : IEntityTypeConfiguration<LichSuTrangThai>
{
    public void Configure(EntityTypeBuilder<LichSuTrangThai> b)
    {
        b.ToUpmTable("LichSuTrangThai");
        b.Property(x => x.TuTrangThai).HasMaxLength(30);
        b.Property(x => x.DenTrangThai).HasMaxLength(30);
        b.Property(x => x.LyDo).Str(500);
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId, s => s.LichSuTrangThais);
        b.HasFk(x => x.NguoiThucHien, x => x.NguoiThucHienId);
        b.HasFk(x => x.QuyetDinh, x => x.QuyetDinhId);
        b.HasIndex(x => new { x.SVTrongDotId, x.ThoiDiem });
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToUpmTable("AuditLog");
        b.Property(x => x.LoaiDoiTuong).Str(50);
        b.Property(x => x.GiaTriCu).StrMax();
        b.Property(x => x.GiaTriMoi).StrMax();
        b.Property(x => x.SoQDBatBuoc).Str(50);
        b.Property(x => x.IpAddress).Str(45);
        b.HasFk(x => x.NguoiDung, x => x.NguoiDungId);
        // CoSoId: không FK (log phải ghi được kể cả khi dữ liệu gốc thay đổi) – chỉ index để lọc theo cơ sở.
        b.HasIndex(x => new { x.CoSoId, x.ThoiDiem });
        b.HasIndex(x => new { x.LoaiDoiTuong, x.DoiTuongId });
    }
}

internal sealed class LichNhacViecConfiguration : IEntityTypeConfiguration<LichNhacViec>
{
    public void Configure(EntityTypeBuilder<LichNhacViec> b)
    {
        b.ToUpmTable("LichNhacViec");
        b.HasFk(x => x.SVTrongDot, x => x.SVTrongDotId);
        // Job nhắc việc quét các dòng chưa gửi đến hạn.
        b.HasIndex(x => new { x.DaGui, x.HanNgay });
        // UC-3a: không gửi trùng – một hồ sơ, một loại nhắc, một hạn chỉ có một bản ghi.
        b.HasIndex(x => new { x.SVTrongDotId, x.LoaiNhac, x.HanNgay }).IsUnique();
    }
}
