using UniProject.Share.Domain;

namespace UniProject.API.Modules.Project
{
    public enum ProjectStatus
    {
        CHO_THUC_HIEN,
        DANG_THUC_HIEN,
        CHO_BAO_VE,
        DA_BAO_VE,
        HOAN_THANH,
        TAM_DUNG,
        DA_HUY
    }

    public enum ProjectMemberRole
    {
        TRUONG_NHOM,
        THANH_VIEN
    }

    public enum AdvisorType
    {
        CHINH,
        PHU
    }

    public enum ProgressEvaluation
    {
        KHONG_DAT,
        CAN_CAI_THIEN,
        DAT,
        TOT,
        RAT_TOT
    }

    public enum RevisionStatus
    {
        CHUA_THUC_HIEN,
        DANG_THUC_HIEN,
        DA_HOAN_THANH,
        DA_XAC_NHAN,
        DA_HUY
    }

    public enum DocumentType
    {
        DE_CUONG,
        BAO_CAO_TIEN_DO,
        BAO_CAO,
        SLIDE,
        SOURCE_CODE,
        PHU_LUC,
        KHAC
    }

    public class DoAn : AuditableEntity<int>
    {
        public string MaDoAnCode { get; private set; } = string.Empty;
        public string TenDoAn { get; private set; } = string.Empty;
        public int MaDotDoAn { get; private set; }
        public int MaDeTai { get; private set; }
        public int? MaDangKy { get; private set; }
        public DateTime NgayTao { get; private set; } = DateTime.UtcNow;
        public DateTime? NgayBatDau { get; private set; }
        public DateTime? NgayKetThuc { get; private set; }
        public ProjectStatus TrangThai { get; private set; } = ProjectStatus.CHO_THUC_HIEN;

        public virtual ICollection<ThanhVienDoAn> ThanhViens { get; private set; } = new List<ThanhVienDoAn>();
        public virtual ICollection<PhanCongHuongDan> PhanCongHuongDans { get; private set; } = new List<PhanCongHuongDan>();
        public virtual ICollection<BaoCaoTienDo> BaoCaoTienDos { get; private set; } = new List<BaoCaoTienDo>();
        public virtual ICollection<YeuCauChinhSua> YeuCauChinhSuas { get; private set; } = new List<YeuCauChinhSua>();
        public virtual ICollection<TaiLieuDoAn> TaiLieuDoAns { get; private set; } = new List<TaiLieuDoAn>();

        private DoAn() { }

        public DoAn(string maDoAnCode, string tenDoAn, int maDotDoAn, int maDeTai, int? maDangKy = null)
        {
            MaDoAnCode = maDoAnCode.Trim().ToUpperInvariant();
            TenDoAn = tenDoAn.Trim();
            MaDotDoAn = maDotDoAn;
            MaDeTai = maDeTai;
            MaDangKy = maDangKy;
            NgayTao = DateTime.UtcNow;
            TrangThai = ProjectStatus.CHO_THUC_HIEN;
        }

        public void AddMember(int studentId, ProjectMemberRole role, int maxStudents)
        {
            if (ThanhViens.Count >= maxStudents)
                throw new DomainException($"Số lượng thành viên vượt quá giới hạn ({maxStudents}) của đề tài.");

            if (ThanhViens.Any(x => x.MaSV == studentId))
                throw new DomainException("Sinh viên này đã có trong đồ án.");

            if (role == ProjectMemberRole.TRUONG_NHOM && ThanhViens.Any(x => x.VaiTro == ProjectMemberRole.TRUONG_NHOM))
                throw new DomainException("Đồ án chỉ có duy nhất 1 trưởng nhóm.");

            ThanhViens.Add(new ThanhVienDoAn(Id, studentId, role));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Đổi đề tài theo yêu cầu đổi đề tài đã được duyệt.</summary>
        public void ChangeTopic(int topicId)
        {
            if (TrangThai is ProjectStatus.DA_HUY or ProjectStatus.HOAN_THANH)
                throw new DomainException($"Đồ án đang ở trạng thái {TrangThai}, không thể đổi đề tài.");
            MaDeTai = topicId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Sinh viên chuyển sang nhóm khác. Đồ án không còn ai thì chuyển DA_HUY.</summary>
        public void RemoveMember(int studentId)
        {
            var member = ThanhViens.FirstOrDefault(x => x.MaSV == studentId)
                ?? throw new DomainException("Sinh viên không thuộc đồ án này.");
            ThanhViens.Remove(member);
            if (ThanhViens.Count == 0)
                ChangeStatus(ProjectStatus.DA_HUY);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Đồ án còn thành viên mà không có trưởng nhóm thì người có mã nhỏ nhất làm trưởng nhóm.</summary>
        public void EnsureLeader()
        {
            if (ThanhViens.Count == 0 || ThanhViens.Any(x => x.VaiTro == ProjectMemberRole.TRUONG_NHOM))
                return;
            ThanhViens.OrderBy(x => x.Id).First().ChangeRole(ProjectMemberRole.TRUONG_NHOM);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignAdvisor(int lecturerId, AdvisorType type)
        {
            if (type == AdvisorType.CHINH && PhanCongHuongDans.Any(x => x.LoaiHuongDan == AdvisorType.CHINH && x.TrangThai))
                throw new DomainException("Đồ án đã có giảng viên hướng dẫn chính đang phụ trách.");

            if (PhanCongHuongDans.Any(x => x.MaGV == lecturerId && x.TrangThai))
                throw new DomainException("Giảng viên này đã được phân công hướng dẫn đồ án.");

            PhanCongHuongDans.Add(new PhanCongHuongDan(Id, lecturerId, type));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Tự ghi ngày bắt đầu khi chuyển sang DANG_THUC_HIEN, ngày kết thúc khi HOAN_THANH / DA_BAO_VE.
        /// </summary>
        public void ChangeStatus(ProjectStatus newStatus)
        {
            TrangThai = newStatus;
            if (newStatus == ProjectStatus.DANG_THUC_HIEN && NgayBatDau == null)
                NgayBatDau = DateTime.UtcNow;
            if (newStatus is ProjectStatus.HOAN_THANH or ProjectStatus.DA_BAO_VE && NgayKetThuc == null)
                NgayKetThuc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class ThanhVienDoAn : Entity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaSV { get; private set; }
        public ProjectMemberRole VaiTro { get; private set; }

        public virtual DoAn DoAn { get; private set; } = null!;

        private ThanhVienDoAn() { }

        public ThanhVienDoAn(int maDoAn, int maSV, ProjectMemberRole vaiTro)
        {
            MaDoAn = maDoAn;
            MaSV = maSV;
            VaiTro = vaiTro;
        }

        public void ChangeRole(ProjectMemberRole newRole)
        {
            VaiTro = newRole;
        }
    }

    public class PhanCongHuongDan : Entity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaGV { get; private set; }
        public AdvisorType LoaiHuongDan { get; private set; }
        public DateTime NgayPhanCong { get; private set; } = DateTime.UtcNow;
        public bool TrangThai { get; private set; } = true;

        public virtual DoAn DoAn { get; private set; } = null!;

        private PhanCongHuongDan() { }

        public PhanCongHuongDan(int maDoAn, int maGV, AdvisorType loaiHuongDan)
        {
            MaDoAn = maDoAn;
            MaGV = maGV;
            LoaiHuongDan = loaiHuongDan;
            NgayPhanCong = DateTime.UtcNow;
            TrangThai = true;
        }

        public void Deactivate()
        {
            TrangThai = false;
        }
    }

    public class BaoCaoTienDo : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaSVNop { get; private set; }
        public int LanBaoCao { get; private set; }
        public string TieuDe { get; private set; } = string.Empty;
        public string NoiDung { get; private set; } = string.Empty;
        public string? CongViecDaLam { get; private set; }
        public string? CongViecTiepTheo { get; private set; }
        public string? KhoKhan { get; private set; }
        public int PhanTramHoanThanh { get; private set; }
        public DateTime NgayNop { get; private set; } = DateTime.UtcNow;

        public virtual DoAn DoAn { get; private set; } = null!;
        public virtual ICollection<NhanXetTienDo> NhanXetTienDos { get; private set; } = new List<NhanXetTienDo>();

        private BaoCaoTienDo() { }

        public BaoCaoTienDo(
            int maDoAn,
            int maSVNop,
            int lanBaoCao,
            string tieuDe,
            string noiDung,
            string? congViecDaLam,
            string? congViecTiepTheo,
            string? khoKhan,
            int phanTramHoanThanh)
        {
            if (phanTramHoanThanh < 0 || phanTramHoanThanh > 100)
                throw new DomainException("Phần trăm hoàn thành phải từ 0 đến 100.");

            MaDoAn = maDoAn;
            MaSVNop = maSVNop;
            LanBaoCao = lanBaoCao;
            TieuDe = tieuDe.Trim();
            NoiDung = noiDung.Trim();
            CongViecDaLam = congViecDaLam?.Trim();
            CongViecTiepTheo = congViecTiepTheo?.Trim();
            KhoKhan = khoKhan?.Trim();
            PhanTramHoanThanh = phanTramHoanThanh;
            NgayNop = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class NhanXetTienDo : Entity<int>
    {
        public int MaBaoCao { get; private set; }
        public int MaGV { get; private set; }
        public string NoiDungNhanXet { get; private set; } = string.Empty;
        public ProgressEvaluation MucDanhGia { get; private set; }
        public DateTime NgayNhanXet { get; private set; } = DateTime.UtcNow;

        public virtual BaoCaoTienDo BaoCaoTienDo { get; private set; } = null!;

        private NhanXetTienDo() { }

        public NhanXetTienDo(int maBaoCao, int maGV, string noiDungNhanXet, ProgressEvaluation mucDanhGia)
        {
            MaBaoCao = maBaoCao;
            MaGV = maGV;
            NoiDungNhanXet = noiDungNhanXet.Trim();
            MucDanhGia = mucDanhGia;
            NgayNhanXet = DateTime.UtcNow;
        }
    }

    public class YeuCauChinhSua : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaGVYeuCau { get; private set; }
        public string TieuDe { get; private set; } = string.Empty;
        public string NoiDung { get; private set; } = string.Empty;
        public DateTime? HanHoanThanh { get; private set; }
        public DateTime NgayYeuCau { get; private set; } = DateTime.UtcNow;
        public RevisionStatus TrangThai { get; private set; } = RevisionStatus.CHUA_THUC_HIEN;

        public virtual DoAn DoAn { get; private set; } = null!;

        private YeuCauChinhSua() { }

        public YeuCauChinhSua(int maDoAn, int maGVYeuCau, string tieuDe, string noiDung, DateTime? hanHoanThanh)
        {
            MaDoAn = maDoAn;
            MaGVYeuCau = maGVYeuCau;
            TieuDe = tieuDe.Trim();
            NoiDung = noiDung.Trim();
            HanHoanThanh = hanHoanThanh;
            NgayYeuCau = DateTime.UtcNow;
            TrangThai = RevisionStatus.CHUA_THUC_HIEN;
            CreatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(RevisionStatus newStatus)
        {
            TrangThai = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class TaiLieuDoAn : Entity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaSVNop { get; private set; }
        public string TenTaiLieu { get; private set; } = string.Empty;
        public string DuongDanFile { get; private set; } = string.Empty;
        public DocumentType LoaiTaiLieu { get; private set; }
        public int Version { get; private set; } = 1;
        public DateTime NgayUpload { get; private set; } = DateTime.UtcNow;

        public virtual DoAn DoAn { get; private set; } = null!;

        private TaiLieuDoAn() { }

        public TaiLieuDoAn(int maDoAn, int maSVNop, string tenTaiLieu, string duongDanFile, DocumentType loaiTaiLieu, int version)
        {
            MaDoAn = maDoAn;
            MaSVNop = maSVNop;
            TenTaiLieu = tenTaiLieu.Trim();
            DuongDanFile = duongDanFile.Trim();
            LoaiTaiLieu = loaiTaiLieu;
            Version = version;
            NgayUpload = DateTime.UtcNow;
        }
    }
}
