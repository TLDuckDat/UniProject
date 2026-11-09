// ERD-04 · Đề tài, phân công, tiến độ (docs/thiet-ke.md).
using Upm.Domain.Common;
using Upm.Domain.Enums;

namespace Upm.Domain.Entities;

public class MocTienDo : AuditableEntity
{
    public long DotId { get; set; }
    public DotDATN Dot { get; set; } = null!;
    public MaMocTienDo MaMoc { get; set; }
    public byte TuanTu { get; set; }
    public byte TuanDen { get; set; }
    public DateOnly NgayHan { get; set; }
    /// <summary>Chỉ dùng cho M2 (60–70%).</summary>
    public decimal? YeuCauToiThieuPct { get; set; }
    public string? MoTa { get; set; }
}

public class DeTai : AuditableEntity
{
    public long DotId { get; set; }
    public DotDATN Dot { get; set; } = null!;
    public long BoMonId { get; set; }
    public BoMon BoMon { get; set; } = null!;
    public string TenDeTai { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public LoaiDeTai LoaiDeTai { get; set; }
    /// <summary>Mặc định 1, tối đa 3.</summary>
    public byte MaxSV { get; set; } = 1;
    public NguonDeXuat NguonDeXuat { get; set; }
    public long? GVDeXuatId { get; set; }
    public GiangVien? GVDeXuat { get; set; }
    public long? SVDeXuatId { get; set; }
    public SinhVien? SVDeXuat { get; set; }
    public TrangThaiDuyetDeTai TrangThaiDuyet { get; set; }
    public long? NguoiDuyetId { get; set; }
    public TaiKhoan? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public bool CanhBaoTrung { get; set; }

    /// <summary>Các hồ sơ SV làm đề tài này (nhóm 2–3 SV).</summary>
    public ICollection<SVTrongDot> SVTrongDots { get; set; } = [];
}

public class BaoCaoTienDo : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long MocTienDoId { get; set; }
    public MocTienDo MocTienDo { get; set; } = null!;
    public DateTime NgayNop { get; set; }
    /// <summary>File qua IFileStorage (chưa có bảng File → không FK).</summary>
    public long? FileId { get; set; }
    public decimal TyLeHoanThanhPct { get; set; }
    public string? GVHDNhanXet { get; set; }
    public decimal? DiemTienDo { get; set; }
    public KetQuaTienDo KetQua { get; set; }
    public DateTime? CanhBaoGuiLuc { get; set; }
    /// <summary>C07: SV xác nhận không dùng AI viết hộ / bịa số liệu khi nộp báo cáo (UC-3a).</summary>
    public bool CamKetAI { get; set; }
    public DateTime? ThoiDiemCamKetAI { get; set; }
}

public class DangKyNguyenVong : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long DeTaiId { get; set; }
    public DeTai DeTai { get; set; } = null!;
    public byte ThuTuNguyenVong { get; set; }
    public TrangThaiNguyenVong TrangThai { get; set; }
    public DateTime NgayDangKy { get; set; }
}

/// <summary>GV tự khai báo xung đột lợi ích với một SV (B07). Không có trong thiet-ke.md – bổ sung khi làm UC-2.</summary>
public class KhaiBaoXungDot : AuditableEntity
{
    public long GiangVienId { get; set; }
    public GiangVien GiangVien { get; set; } = null!;
    public long SinhVienId { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public string? LyDo { get; set; }
}

/// <summary>Vai trò GV trên từng SV. Chặn cứng GVHD = GVĐD (B06).</summary>
public class PhanCong : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public long GiangVienId { get; set; }
    public GiangVien GiangVien { get; set; } = null!;
    public VaiTroPhanCong VaiTro { get; set; }
    public DateOnly NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public string? LyDoThayDoi { get; set; }
    public long? QuyetDinhId { get; set; }
    public QuyetDinh? QuyetDinh { get; set; }
    public bool DuocNhapDiem { get; set; }
    public bool TuKhaiXungDot { get; set; }
    public long? VuotQuotaDuyetBoiId { get; set; }
    public TaiKhoan? VuotQuotaDuyetBoi { get; set; }
}

public class PhieuGiaoNhiemVu : AuditableEntity
{
    public long SVTrongDotId { get; set; }
    public SVTrongDot SVTrongDot { get; set; } = null!;
    public string NoiDungNhiemVu { get; set; } = string.Empty;
    public string? YeuCauSanPham { get; set; }
    public DateOnly NgayGiao { get; set; }
    public long? FileId { get; set; }
}
