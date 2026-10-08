using UniProject.Share.Domain;

namespace UniProject.API.Modules.Defense
{
    public enum ReviewStatus
    {
        CHUA_PHAN_BIEN,
        DANG_PHAN_BIEN,
        DA_PHAN_BIEN
    }

    public enum CommitteeStatus
    {
        CHUA_BAO_VE,
        DANG_BAO_VE,
        DA_KET_THUC,
        DA_HUY
    }

    public enum CommitteeRole
    {
        CHU_TICH,
        THU_KY,
        UY_VIEN
    }

    public enum ScheduleStatus
    {
        LEN_LICH,
        DANG_BAO_VE,
        HOAN_THANH,
        DA_HUY
    }

    public class PhanBienDoAn : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaGV { get; private set; }
        public DateTime NgayPhanCong { get; private set; } = DateTime.UtcNow;
        public ReviewStatus TrangThai { get; private set; } = ReviewStatus.CHUA_PHAN_BIEN;

        private PhanBienDoAn() { }

        public PhanBienDoAn(int maDoAn, int maGV)
        {
            MaDoAn = maDoAn;
            MaGV = maGV;
            NgayPhanCong = DateTime.UtcNow;
            TrangThai = ReviewStatus.CHUA_PHAN_BIEN;
        }

        public void ChangeStatus(ReviewStatus status)
        {
            TrangThai = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Hội đồng bảo vệ theo quy chế đào tạo (QĐ 2006/QĐ-ĐHGTVT): 3 đến 5 thành viên,
    /// gồm đúng một Chủ tịch, đúng một Thư ký, còn lại là Ủy viên.
    /// </summary>
    public class HoiDong : AuditableEntity<int>
    {
        public const int MinMembers = 3;
        public const int MaxMembers = 5;

        public string MaHoiDongCode { get; private set; } = string.Empty;
        public string TenHoiDong { get; private set; } = string.Empty;
        public int MaDotDoAn { get; private set; }
        public string? DiaDiem { get; private set; }
        public DateTime NgayTao { get; private set; } = DateTime.UtcNow;
        public CommitteeStatus TrangThai { get; private set; } = CommitteeStatus.CHUA_BAO_VE;

        public virtual ICollection<ThanhVienHoiDong> ThanhViens { get; private set; } = new List<ThanhVienHoiDong>();
        public virtual ICollection<LichBaoVe> LichBaoVes { get; private set; } = new List<LichBaoVe>();

        private HoiDong() { }

        public HoiDong(string maHoiDongCode, string tenHoiDong, int maDotDoAn, string? diaDiem = null)
        {
            MaHoiDongCode = maHoiDongCode.Trim().ToUpperInvariant();
            TenHoiDong = tenHoiDong.Trim();
            MaDotDoAn = maDotDoAn;
            DiaDiem = diaDiem?.Trim();
            NgayTao = DateTime.UtcNow;
            TrangThai = CommitteeStatus.CHUA_BAO_VE;
        }

        public void AddMember(int lecturerId, CommitteeRole role)
        {
            if (ThanhViens.Any(x => x.MaGV == lecturerId))
                throw new DomainException("Giảng viên này đã là thành viên của hội đồng.");

            if (ThanhViens.Count >= MaxMembers)
                throw new DomainException($"Hội đồng có tối đa {MaxMembers} thành viên.");

            if (role == CommitteeRole.CHU_TICH && ThanhViens.Any(x => x.VaiTro == CommitteeRole.CHU_TICH))
                throw new DomainException("Hội đồng chỉ được có duy nhất 1 Chủ tịch.");

            if (role == CommitteeRole.THU_KY && ThanhViens.Any(x => x.VaiTro == CommitteeRole.THU_KY))
                throw new DomainException("Hội đồng chỉ được có duy nhất 1 Thư ký.");

            ThanhViens.Add(new ThanhVienHoiDong(Id, lecturerId, role));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Các lý do hội đồng chưa đủ điều kiện bảo vệ; rỗng nghĩa là hợp lệ.</summary>
        public static IReadOnlyList<string> GetCompositionErrors(IEnumerable<CommitteeRole> roles)
        {
            var list = roles.ToList();
            var errors = new List<string>();
            if (list.Count < MinMembers || list.Count > MaxMembers)
                errors.Add($"Hội đồng phải có từ {MinMembers} đến {MaxMembers} thành viên (hiện có {list.Count}).");
            var chairs = list.Count(r => r == CommitteeRole.CHU_TICH);
            if (chairs != 1)
                errors.Add($"Hội đồng phải có đúng 1 Chủ tịch (hiện có {chairs}).");
            var secretaries = list.Count(r => r == CommitteeRole.THU_KY);
            if (secretaries != 1)
                errors.Add($"Hội đồng phải có đúng 1 Thư ký (hiện có {secretaries}).");
            return errors;
        }

        public bool IsReadyForDefense => GetCompositionErrors(ThanhViens.Select(x => x.VaiTro)).Count == 0;

        /// <summary>Chỉ xếp lịch bảo vệ khi hội đồng đủ thành phần theo quy chế.</summary>
        public void EnsureReadyForDefense()
        {
            var errors = GetCompositionErrors(ThanhViens.Select(x => x.VaiTro));
            if (errors.Count > 0)
                throw new DomainException("Hội đồng chưa đủ điều kiện bảo vệ: " + string.Join(" ", errors));
        }

        public void ChangeStatus(CommitteeStatus newStatus)
        {
            TrangThai = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class ThanhVienHoiDong : Entity<int>
    {
        public int MaHoiDong { get; private set; }
        public int MaGV { get; private set; }
        public CommitteeRole VaiTro { get; private set; }

        public virtual HoiDong HoiDong { get; private set; } = null!;

        private ThanhVienHoiDong() { }

        public ThanhVienHoiDong(int maHoiDong, int maGV, CommitteeRole vaiTro)
        {
            MaHoiDong = maHoiDong;
            MaGV = maGV;
            VaiTro = vaiTro;
        }
    }

    public class LichBaoVe : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaHoiDong { get; private set; }
        public int MaDotDoAn { get; private set; }
        public DateTime NgayBaoVe { get; private set; }
        public TimeSpan GioBatDau { get; private set; }
        public TimeSpan GioKetThuc { get; private set; }
        public string Phong { get; private set; } = string.Empty;
        public ScheduleStatus TrangThai { get; private set; } = ScheduleStatus.LEN_LICH;

        public virtual HoiDong HoiDong { get; private set; } = null!;

        private LichBaoVe() { }

        public LichBaoVe(
            int maDoAn,
            int maHoiDong,
            int maDotDoAn,
            DateTime ngayBaoVe,
            TimeSpan gioBatDau,
            TimeSpan gioKetThuc,
            string phong)
        {
            if (gioKetThuc <= gioBatDau)
                throw new DomainException("Giờ kết thúc phải sau giờ bắt đầu.");

            MaDoAn = maDoAn;
            MaHoiDong = maHoiDong;
            MaDotDoAn = maDotDoAn;
            NgayBaoVe = ngayBaoVe.Date;
            GioBatDau = gioBatDau;
            GioKetThuc = gioKetThuc;
            Phong = phong.Trim();
            TrangThai = ScheduleStatus.LEN_LICH;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(DateTime ngayBaoVe, TimeSpan gioBatDau, TimeSpan gioKetThuc, string phong)
        {
            if (gioKetThuc <= gioBatDau)
                throw new DomainException("Giờ kết thúc phải sau giờ bắt đầu.");

            NgayBaoVe = ngayBaoVe.Date;
            GioBatDau = gioBatDau;
            GioKetThuc = gioKetThuc;
            Phong = phong.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(ScheduleStatus status)
        {
            TrangThai = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
