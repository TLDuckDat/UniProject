using UniProject.Share.Domain;

namespace UniProject.API.Modules.Registration
{
    public enum RegistrationStatus
    {
        NHAP,
        CHO_DUYET,
        DA_DUYET,
        TU_CHOI,
        DA_HUY
    }

    public enum GroupMemberRole
    {
        TRUONG_NHOM,
        THANH_VIEN
    }

    public enum PreferenceStatus
    {
        CHO_DUYET,
        DUOC_CHON,
        TU_CHOI
    }

    public enum ChangeRequestType
    {
        DOI_DE_TAI,
        DOI_NHOM
    }

    public enum ChangeRequestStatus
    {
        CHO_DUYET,
        DA_DUYET,
        TU_CHOI
    }

    public class DangKyDeTai : AuditableEntity<int>
    {
        public int MaDotDoAn { get; private set; }
        public string? TenNhom { get; private set; }
        public DateTime NgayDangKy { get; private set; } = DateTime.UtcNow;
        public RegistrationStatus TrangThai { get; private set; } = RegistrationStatus.NHAP;

        public virtual ICollection<ThanhVienDangKy> ThanhViens { get; private set; } = new List<ThanhVienDangKy>();
        public virtual ICollection<ChiTietDangKy> ChiTietDangKys { get; private set; } = new List<ChiTietDangKy>();

        private DangKyDeTai() { }

        public DangKyDeTai(int maDotDoAn, string? tenNhom = null)
        {
            MaDotDoAn = maDotDoAn;
            TenNhom = tenNhom?.Trim();
            NgayDangKy = DateTime.UtcNow;
            TrangThai = RegistrationStatus.NHAP;
        }

        public void AddMember(int studentId, GroupMemberRole role)
        {
            if (TrangThai != RegistrationStatus.NHAP)
                throw new DomainException("Chỉ có thể thêm thành viên khi đơn đăng ký đang ở trạng thái Nháp.");

            if (ThanhViens.Any(x => x.MaSV == studentId))
                throw new DomainException("Sinh viên này đã có trong nhóm.");

            if (role == GroupMemberRole.TRUONG_NHOM && ThanhViens.Any(x => x.VaiTro == GroupMemberRole.TRUONG_NHOM))
                throw new DomainException("Nhóm chỉ được có duy nhất 1 trưởng nhóm.");

            ThanhViens.Add(new ThanhVienDangKy(Id, studentId, role));
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveMember(int studentId)
        {
            if (TrangThai != RegistrationStatus.NHAP)
                throw new DomainException("Chỉ có thể xóa thành viên khi đơn đăng ký đang ở trạng thái Nháp.");

            var member = ThanhViens.FirstOrDefault(x => x.MaSV == studentId);
            if (member == null)
                throw new DomainException("Không tìm thấy sinh viên trong nhóm.");

            ThanhViens.Remove(member);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddPreference(int periodTopicId, int priorityOrder)
        {
            if (TrangThai != RegistrationStatus.NHAP)
                throw new DomainException("Chỉ có thể thêm nguyện vọng khi đơn đăng ký đang ở trạng thái Nháp.");

            if (priorityOrder < 1 || priorityOrder > 10)
                throw new DomainException("Thứ tự ưu tiên phải từ 1 đến 10.");

            if (ChiTietDangKys.Any(x => x.MaDeTaiDotDoAn == periodTopicId))
                throw new DomainException("Đề tài này đã được chọn trong danh sách nguyện vọng.");

            if (ChiTietDangKys.Any(x => x.ThuTuUuTien == priorityOrder))
                throw new DomainException($"Thứ tự ưu tiên số {priorityOrder} đã tồn tại.");

            ChiTietDangKys.Add(new ChiTietDangKy(Id, periodTopicId, priorityOrder));
            UpdatedAt = DateTime.UtcNow;
        }

        public void Submit()
        {
            if (TrangThai != RegistrationStatus.NHAP)
                throw new DomainException("Đơn đăng ký không ở trạng thái Nháp.");

            if (!ThanhViens.Any())
                throw new DomainException("Nhóm phải có ít nhất 1 thành viên.");

            if (ThanhViens.Count > 1 && !ThanhViens.Any(x => x.VaiTro == GroupMemberRole.TRUONG_NHOM))
                throw new DomainException("Nhóm có nhiều thành viên bắt buộc phải có đúng 1 trưởng nhóm.");

            if (!ChiTietDangKys.Any())
                throw new DomainException("Phải chọn ít nhất 1 đề tài nguyện vọng.");

            TrangThai = RegistrationStatus.CHO_DUYET;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Approve()
        {
            if (TrangThai != RegistrationStatus.CHO_DUYET)
                throw new DomainException("Chỉ duyệt được đơn đang chờ duyệt.");
            TrangThai = RegistrationStatus.DA_DUYET;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (TrangThai != RegistrationStatus.CHO_DUYET)
                throw new DomainException("Chỉ từ chối được đơn đang chờ duyệt.");
            TrangThai = RegistrationStatus.TU_CHOI;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (TrangThai is not (RegistrationStatus.NHAP or RegistrationStatus.CHO_DUYET))
                throw new DomainException("Chỉ hủy được đơn đang nháp hoặc chờ duyệt.");
            TrangThai = RegistrationStatus.DA_HUY;
            UpdatedAt = DateTime.UtcNow;
        }

        // ----- Thay đổi sau khi đơn đã duyệt (qua yêu cầu đổi đề tài / đổi nhóm được Admin duyệt) -----

        private void EnsureApproved()
        {
            if (TrangThai != RegistrationStatus.DA_DUYET)
                throw new DomainException("Chỉ thay đổi được đơn đăng ký đã duyệt.");
        }

        public ChiTietDangKy? SelectedPreference => ChiTietDangKys.FirstOrDefault(x => x.TrangThai == PreferenceStatus.DUOC_CHON);

        /// <summary>Bước 1 đổi đề tài: bỏ chọn nguyện vọng đang được chọn (lưu trước để không vướng index một nguyện vọng DUOC_CHON).</summary>
        public void ReleaseSelectedTopic(string note)
        {
            EnsureApproved();
            var current = SelectedPreference ?? throw new DomainException("Đơn đăng ký chưa có đề tài được chọn.");
            current.Reject(null, note);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Bước 2 đổi đề tài: chọn đề tài mới; dùng lại nguyện vọng cũ nếu có, không thì thêm nguyện vọng mới cuối danh sách.</summary>
        public void SelectTopic(int periodTopicId, int? deciderId)
        {
            EnsureApproved();
            if (SelectedPreference != null)
                throw new DomainException("Đơn đăng ký đang có đề tài được chọn.");

            var pref = ChiTietDangKys.FirstOrDefault(x => x.MaDeTaiDotDoAn == periodTopicId);
            if (pref == null)
            {
                var nextOrder = ChiTietDangKys.Count == 0 ? 1 : ChiTietDangKys.Max(x => x.ThuTuUuTien) + 1;
                if (nextOrder > 10)
                    throw new DomainException("Đơn đã có tối đa 10 nguyện vọng, không thêm được đề tài mới.");
                pref = new ChiTietDangKy(Id, periodTopicId, nextOrder);
                ChiTietDangKys.Add(pref);
            }
            pref.Select(deciderId);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Đổi nhóm, phía nhóm cũ: rời nhóm. Nhóm không còn ai thì đơn chuyển DA_HUY.</summary>
        public void TransferOut(int studentId)
        {
            EnsureApproved();
            var member = ThanhViens.FirstOrDefault(x => x.MaSV == studentId)
                ?? throw new DomainException("Không tìm thấy sinh viên trong nhóm.");
            ThanhViens.Remove(member);
            if (ThanhViens.Count == 0)
                TrangThai = RegistrationStatus.DA_HUY;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Nhóm còn thành viên mà không có trưởng nhóm (trưởng nhóm vừa chuyển đi) thì người vào sớm nhất làm trưởng nhóm.</summary>
        public void EnsureLeader()
        {
            if (ThanhViens.Count == 0 || ThanhViens.Any(x => x.VaiTro == GroupMemberRole.TRUONG_NHOM))
                return;
            ThanhViens.OrderBy(x => x.NgayThamGia).ThenBy(x => x.Id).First().ChangeRole(GroupMemberRole.TRUONG_NHOM);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Đổi nhóm, phía nhóm mới: nhận thêm thành viên, không vượt quy mô cho phép.</summary>
        public void TransferIn(int studentId, int maxMembers)
        {
            EnsureApproved();
            if (ThanhViens.Any(x => x.MaSV == studentId))
                throw new DomainException("Sinh viên này đã có trong nhóm.");
            if (ThanhViens.Count >= maxMembers)
                throw new DomainException($"Nhóm đã đủ {maxMembers} thành viên.");
            ThanhViens.Add(new ThanhVienDangKy(Id, studentId, GroupMemberRole.THANH_VIEN));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Yêu cầu đổi đề tài (cả nhóm, do trưởng nhóm gửi) hoặc đổi nhóm (một sinh viên chuyển sang nhóm khác cùng đợt)
    /// sau khi đơn đăng ký đã duyệt. Admin duyệt hoặc từ chối.
    /// </summary>
    public class YeuCauThayDoi : AuditableEntity<int>
    {
        public int MaDangKy { get; private set; }
        public int MaSVYeuCau { get; private set; }
        public ChangeRequestType LoaiYeuCau { get; private set; }
        public int? MaDeTaiDotDoAnMoi { get; private set; }
        public int? MaDangKyMoi { get; private set; }
        public string LyDo { get; private set; } = string.Empty;
        public ChangeRequestStatus TrangThai { get; private set; } = ChangeRequestStatus.CHO_DUYET;
        public DateTime NgayYeuCau { get; private set; } = DateTime.UtcNow;
        public int? MaTaiKhoanXuLy { get; private set; }
        public DateTime? NgayXuLy { get; private set; }
        public string? GhiChuXuLy { get; private set; }

        private YeuCauThayDoi() { }

        public YeuCauThayDoi(int maDangKy, int maSVYeuCau, ChangeRequestType loai, int? maDeTaiDotDoAnMoi, int? maDangKyMoi, string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new DomainException("Phải nêu lý do yêu cầu thay đổi.");
            if (loai == ChangeRequestType.DOI_DE_TAI && (!maDeTaiDotDoAnMoi.HasValue || maDangKyMoi.HasValue))
                throw new DomainException("Yêu cầu đổi đề tài phải chọn đề tài mới (NewPeriodTopicId) và không chọn nhóm mới.");
            if (loai == ChangeRequestType.DOI_NHOM && (!maDangKyMoi.HasValue || maDeTaiDotDoAnMoi.HasValue))
                throw new DomainException("Yêu cầu đổi nhóm phải chọn nhóm mới (TargetRegistrationId) và không chọn đề tài mới.");
            if (loai == ChangeRequestType.DOI_NHOM && maDangKyMoi == maDangKy)
                throw new DomainException("Nhóm mới phải khác nhóm hiện tại.");

            MaDangKy = maDangKy;
            MaSVYeuCau = maSVYeuCau;
            LoaiYeuCau = loai;
            MaDeTaiDotDoAnMoi = maDeTaiDotDoAnMoi;
            MaDangKyMoi = maDangKyMoi;
            LyDo = lyDo.Trim();
            TrangThai = ChangeRequestStatus.CHO_DUYET;
            NgayYeuCau = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public void Approve(int accountId, string? note)
        {
            if (TrangThai != ChangeRequestStatus.CHO_DUYET)
                throw new DomainException("Yêu cầu này đã được xử lý.");
            TrangThai = ChangeRequestStatus.DA_DUYET;
            MaTaiKhoanXuLy = accountId;
            NgayXuLy = DateTime.UtcNow;
            GhiChuXuLy = note?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject(int accountId, string reason)
        {
            if (TrangThai != ChangeRequestStatus.CHO_DUYET)
                throw new DomainException("Yêu cầu này đã được xử lý.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Lý do từ chối không được để trống.");
            TrangThai = ChangeRequestStatus.TU_CHOI;
            MaTaiKhoanXuLy = accountId;
            NgayXuLy = DateTime.UtcNow;
            GhiChuXuLy = reason.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class ThanhVienDangKy : Entity<int>
    {
        public int MaDangKy { get; private set; }
        public int MaSV { get; private set; }
        public GroupMemberRole VaiTro { get; private set; }
        public DateTime NgayThamGia { get; private set; } = DateTime.UtcNow;

        public virtual DangKyDeTai DangKyDeTai { get; private set; } = null!;

        private ThanhVienDangKy() { }

        public ThanhVienDangKy(int maDangKy, int maSV, GroupMemberRole vaiTro)
        {
            MaDangKy = maDangKy;
            MaSV = maSV;
            VaiTro = vaiTro;
            NgayThamGia = DateTime.UtcNow;
        }

        public void ChangeRole(GroupMemberRole newRole)
        {
            VaiTro = newRole;
        }
    }

    public class ChiTietDangKy : Entity<int>
    {
        public int MaDangKy { get; private set; }
        public int MaDeTaiDotDoAn { get; private set; }
        public int ThuTuUuTien { get; private set; }
        public PreferenceStatus TrangThai { get; private set; } = PreferenceStatus.CHO_DUYET;
        public int? MaGVDuyet { get; private set; }
        public DateTime? NgayDuyet { get; private set; }
        public string? LyDoTuChoi { get; private set; }

        public virtual DangKyDeTai DangKyDeTai { get; private set; } = null!;

        private ChiTietDangKy() { }

        public ChiTietDangKy(int maDangKy, int maDeTaiDotDoAn, int thuTuUuTien)
        {
            if (thuTuUuTien < 1 || thuTuUuTien > 10)
                throw new DomainException("Thứ tự ưu tiên phải từ 1 đến 10.");

            MaDangKy = maDangKy;
            MaDeTaiDotDoAn = maDeTaiDotDoAn;
            ThuTuUuTien = thuTuUuTien;
            TrangThai = PreferenceStatus.CHO_DUYET;
        }

        public void SetPriorityOrder(int newOrder)
        {
            if (newOrder < 1 || newOrder > 10)
                throw new DomainException("Thứ tự ưu tiên phải từ 1 đến 10.");

            ThuTuUuTien = newOrder;
        }

        public void Approve(int? lecturerId)
        {
            if (TrangThai != PreferenceStatus.CHO_DUYET)
                throw new DomainException("Nguyện vọng này đã được xử lý.");
            TrangThai = PreferenceStatus.DUOC_CHON;
            MaGVDuyet = lecturerId;
            NgayDuyet = DateTime.UtcNow;
            LyDoTuChoi = null;
        }

        /// <summary>Chọn nguyện vọng khi Admin duyệt đổi đề tài (kể cả nguyện vọng từng bị từ chối).</summary>
        public void Select(int? deciderId)
        {
            TrangThai = PreferenceStatus.DUOC_CHON;
            MaGVDuyet = deciderId;
            NgayDuyet = DateTime.UtcNow;
            LyDoTuChoi = null;
        }

        public void Reject(int? lecturerId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Lý do từ chối nguyện vọng không được để trống.");

            TrangThai = PreferenceStatus.TU_CHOI;
            MaGVDuyet = lecturerId;
            NgayDuyet = DateTime.UtcNow;
            LyDoTuChoi = reason.Trim();
        }
    }
}
