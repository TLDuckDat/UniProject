using UniProject.API.Modules.ProjectPeriod;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Topic
{
    public enum TopicStatus
    {
        CHO_DUYET,
        DA_DUYET,
        TU_CHOI,
        NGUNG_SU_DUNG
    }

    public enum PeriodTopicStatus
    {
        CHUA_MO,
        DANG_MO,
        DA_DONG,
        TAM_KHOA
    }

    /// <summary>
    /// Đề tài do giảng viên đề xuất, hoặc do sinh viên đề xuất kèm giảng viên dự kiến hướng dẫn
    /// (khi đó MaGVDeXuat là giảng viên dự kiến hướng dẫn, MaSVDeXuat là sinh viên đề xuất).
    /// Vòng đời: CHO_DUYET → DA_DUYET / TU_CHOI → NGUNG_SU_DUNG.
    /// </summary>
    public class DeTai : AuditableEntity<int>
    {
        public string MaDeTaiCode { get; private set; } = string.Empty;
        public string TenDeTai { get; private set; } = string.Empty;
        public string MoTa { get; private set; } = string.Empty;
        public string? MucTieu { get; private set; }
        public string? YeuCau { get; private set; }
        public string? CongNgheDuKien { get; private set; }
        public int SoLuongSinhVienToiDa { get; private set; } = 1;
        public int MaGVDeXuat { get; private set; }
        public int? MaSVDeXuat { get; private set; }
        public int MaChuyenNganh { get; private set; }
        public ProjectKind LoaiDoAn { get; private set; }
        public int? MaGVDuyet { get; private set; }
        public DateTime? NgayDuyet { get; private set; }
        public string? LyDoTuChoi { get; private set; }
        public TopicStatus TrangThai { get; private set; } = TopicStatus.CHO_DUYET;

        public virtual ICollection<DeTaiDotDoAn> DeTaiDotDoAns { get; private set; } = new List<DeTaiDotDoAn>();

        private DeTai() { }

        public DeTai(
            string maDeTaiCode,
            string tenDeTai,
            string moTa,
            string? mucTieu,
            string? yeuCau,
            string? congNgheDuKien,
            int soLuongSinhVienToiDa,
            int maGVDeXuat,
            int maChuyenNganh,
            ProjectKind loaiDoAn,
            int? maSVDeXuat = null)
        {
            if (soLuongSinhVienToiDa < 1 || soLuongSinhVienToiDa > 10)
                throw new DomainException("Số lượng sinh viên tối đa phải từ 1 đến 10.");

            MaDeTaiCode = maDeTaiCode.Trim().ToUpperInvariant();
            TenDeTai = tenDeTai.Trim();
            MoTa = moTa.Trim();
            MucTieu = mucTieu?.Trim();
            YeuCau = yeuCau?.Trim();
            CongNgheDuKien = congNgheDuKien?.Trim();
            SoLuongSinhVienToiDa = soLuongSinhVienToiDa;
            MaGVDeXuat = maGVDeXuat;
            MaSVDeXuat = maSVDeXuat;
            MaChuyenNganh = maChuyenNganh;
            LoaiDoAn = loaiDoAn;
            TrangThai = TopicStatus.CHO_DUYET;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(
            string tenDeTai,
            string moTa,
            string? mucTieu,
            string? yeuCau,
            string? congNgheDuKien,
            int soLuongSinhVienToiDa,
            int maChuyenNganh)
        {
            if (soLuongSinhVienToiDa < 1 || soLuongSinhVienToiDa > 10)
                throw new DomainException("Số lượng sinh viên tối đa phải từ 1 đến 10.");

            if (TrangThai == TopicStatus.DA_DUYET)
                throw new DomainException("Đề tài đã được duyệt không thể chỉnh sửa thông tin trực tiếp.");

            TenDeTai = tenDeTai.Trim();
            MoTa = moTa.Trim();
            MucTieu = mucTieu?.Trim();
            YeuCau = yeuCau?.Trim();
            CongNgheDuKien = congNgheDuKien?.Trim();
            SoLuongSinhVienToiDa = soLuongSinhVienToiDa;
            MaChuyenNganh = maChuyenNganh;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsStudentProposed => MaSVDeXuat.HasValue;

        public void Approve(int? lecturerId)
        {
            if (TrangThai != TopicStatus.CHO_DUYET)
                throw new DomainException("Chỉ duyệt được đề tài đang chờ duyệt.");

            TrangThai = TopicStatus.DA_DUYET;
            MaGVDuyet = lecturerId;
            NgayDuyet = DateTime.UtcNow;
            LyDoTuChoi = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject(int? lecturerId, string reason)
        {
            if (TrangThai != TopicStatus.CHO_DUYET)
                throw new DomainException("Chỉ từ chối được đề tài đang chờ duyệt.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Lý do từ chối đề tài không được để trống.");

            TrangThai = TopicStatus.TU_CHOI;
            MaGVDuyet = lecturerId;
            NgayDuyet = DateTime.UtcNow;
            LyDoTuChoi = reason.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void Disable()
        {
            if (TrangThai == TopicStatus.NGUNG_SU_DUNG)
                throw new DomainException("Đề tài đã ngừng sử dụng.");

            TrangThai = TopicStatus.NGUNG_SU_DUNG;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class DeTaiDotDoAn : AuditableEntity<int>
    {
        public int MaDeTai { get; private set; }
        public int MaDotDoAn { get; private set; }
        public PeriodTopicStatus TrangThai { get; private set; } = PeriodTopicStatus.DANG_MO;

        public virtual DeTai DeTai { get; private set; } = null!;

        private DeTaiDotDoAn() { }

        public DeTaiDotDoAn(int maDeTai, int maDotDoAn, PeriodTopicStatus status = PeriodTopicStatus.DANG_MO)
        {
            MaDeTai = maDeTai;
            MaDotDoAn = maDotDoAn;
            TrangThai = status;
            CreatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(PeriodTopicStatus newStatus)
        {
            TrangThai = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
