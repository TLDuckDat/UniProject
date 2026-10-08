using Microsoft.EntityFrameworkCore;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Topic;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;

namespace UniProject.API.Modules.Registration
{
    /// <summary>
    /// Đăng ký đề tài theo nhóm. Luồng: SV tạo nhóm (NHAP) → trưởng nhóm thêm thành viên &amp; nguyện vọng
    /// → gửi duyệt (CHO_DUYET) → GV ra đề tài duyệt một nguyện vọng (DA_DUYET) hoặc từ chối hết (TU_CHOI).
    /// Quyền: sinh viên chỉ thấy đơn của mình, chỉ trưởng nhóm được sửa đơn; GV/Admin xem được mọi đơn.
    /// </summary>
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _dbContext;
        private readonly CurrentActor _actor;
        private readonly ProjectTypePolicyResolver _policies;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(AppDbContext dbContext, CurrentActor actor, ProjectTypePolicyResolver policies, ILogger<RegistrationService> logger)
        {
            _dbContext = dbContext;
            _actor = actor;
            _policies = policies;
            _logger = logger;
        }

        /// <summary>
        /// Người tạo đơn tự động làm trưởng nhóm. Đợt phải đang mở đăng ký và lớp của SV phải tham gia đợt.
        /// </summary>
        public async Task<RegistrationDto> CreateRegistrationAsync(CreateRegistrationRequest request, CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            await EnsurePeriodOpenAsync(request.PeriodId, cancellationToken);
            await EnsureStudentEligibleAsync(studentId, request.PeriodId, cancellationToken);

            var registration = new DangKyDeTai(request.PeriodId, request.GroupName);
            registration.AddMember(studentId, GroupMemberRole.TRUONG_NHOM);

            _dbContext.DangKyDeTais.Add(registration);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created registration ID {Id} for student ID {StudentId}", registration.Id, studentId);
            return MapToDto(registration);
        }

        public async Task<PagedResult<RegistrationDto>> GetRegistrationsAsync(
            PaginationParams @params,
            int? periodId = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            if (!_actor.IsAdmin && !_actor.IsLecturer)
                throw new ForbiddenException("Sinh viên chỉ xem được đơn của mình (GET /registrations/me).");

            var query = _dbContext.DangKyDeTais
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .Include(x => x.ChiTietDangKys)
                .AsQueryable();

            if (periodId.HasValue)
                query = query.Where(x => x.MaDotDoAn == periodId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = ParseRegistrationStatus(status);
                query = query.Where(x => x.TrangThai == parsed);
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.NgayDangKy)
                : query.OrderBy(x => x.NgayDangKy);

            return await query.ToPagedResultAsync(@params.PageNumber, @params.PageSize, x => MapToDto(x), cancellationToken);
        }

        public async Task<RegistrationDto> GetRegistrationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.DangKyDeTais
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .Include(x => x.ChiTietDangKys)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException("Đăng ký đề tài", id);

            if (!_actor.IsAdmin && !_actor.IsLecturer)
            {
                var studentId = await _actor.GetStudentIdAsync(cancellationToken);
                if (item.ThanhViens.All(m => m.MaSV != studentId))
                    throw new ForbiddenException("Bạn không thuộc nhóm đăng ký này.");
            }

            return MapToDto(item);
        }

        /// <summary>Đơn mới nhất (chưa hủy) mà sinh viên đang đăng nhập tham gia.</summary>
        public async Task<RegistrationDto?> GetMyRegistrationAsync(int? periodId = null, CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            var query = _dbContext.DangKyDeTais
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .Include(x => x.ChiTietDangKys)
                .Where(x => x.ThanhViens.Any(m => m.MaSV == studentId) && x.TrangThai != RegistrationStatus.DA_HUY);

            if (periodId.HasValue)
                query = query.Where(x => x.MaDotDoAn == periodId.Value);

            var item = await query.OrderByDescending(x => x.NgayDangKy).FirstOrDefaultAsync(cancellationToken);
            return item != null ? MapToDto(item) : null;
        }

        public async Task<Result> AddMemberAsync(int registrationId, AddMemberRequest request, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);
            await EnsurePeriodOpenAsync(registration.MaDotDoAn, cancellationToken);
            await EnsureStudentEligibleAsync(request.StudentId, registration.MaDotDoAn, cancellationToken);

            var policy = await _policies.ForPeriodAsync(_dbContext, registration.MaDotDoAn, cancellationToken);
            if (registration.ThanhViens.Count >= policy.MaxMembers)
                throw new BusinessRuleException($"Nhóm {policy.DisplayName} tối đa {policy.MaxMembers} sinh viên.");

            registration.AddMember(request.StudentId, GroupMemberRole.THANH_VIEN);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added student {StudentId} to registration {RegId}", request.StudentId, registrationId);
            return Result.Ok("Thêm thành viên vào nhóm thành công.");
        }

        public async Task<Result> RemoveMemberAsync(int registrationId, int studentId, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);

            if (registration.ThanhViens.Any(m => m.MaSV == studentId && m.VaiTro == GroupMemberRole.TRUONG_NHOM))
                throw new BusinessRuleException("Không thể xóa trưởng nhóm. Muốn giải tán nhóm hãy hủy đơn đăng ký.");

            registration.RemoveMember(studentId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed student {StudentId} from registration {RegId}", studentId, registrationId);
            return Result.Ok("Xóa thành viên khỏi nhóm thành công.");
        }

        /// <summary>Đề tài nguyện vọng phải thuộc cùng đợt với đơn và đang mở đăng ký.</summary>
        public async Task<Result> AddPreferenceAsync(int registrationId, AddPreferenceRequest request, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);
            await EnsurePeriodOpenAsync(registration.MaDotDoAn, cancellationToken);

            var periodTopic = await _dbContext.DeTaiDotDoAns.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.PeriodTopicId, cancellationToken)
                ?? throw new NotFoundException("Đề tài trong đợt đồ án", request.PeriodTopicId);
            if (periodTopic.MaDotDoAn != registration.MaDotDoAn)
                throw new BusinessRuleException("Đề tài này không thuộc đợt đồ án của đơn đăng ký.");
            if (periodTopic.TrangThai != PeriodTopicStatus.DANG_MO)
                throw new BusinessRuleException("Đề tài này hiện không mở đăng ký.");

            registration.AddPreference(request.PeriodTopicId, request.PriorityOrder);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Thêm nguyện vọng thành công.");
        }

        /// <summary>
        /// Nếu thứ tự mới đã thuộc về nguyện vọng khác thì hoán đổi thứ tự của hai nguyện vọng.
        /// </summary>
        public async Task<Result> UpdatePreferenceOrderAsync(int registrationId, UpdatePreferenceOrderRequest request, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);
            if (registration.TrangThai != RegistrationStatus.NHAP)
                throw new BusinessRuleException("Chỉ sắp xếp lại nguyện vọng khi đơn còn ở trạng thái Nháp.");

            var pref = registration.ChiTietDangKys.FirstOrDefault(x => x.Id == request.PreferenceId)
                ?? throw new NotFoundException("Nguyện vọng đăng ký", request.PreferenceId);

            var existing = registration.ChiTietDangKys
                .FirstOrDefault(x => x.ThuTuUuTien == request.NewPriorityOrder && x.Id != request.PreferenceId);
            existing?.SetPriorityOrder(pref.ThuTuUuTien);
            pref.SetPriorityOrder(request.NewPriorityOrder);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Cập nhật thứ tự ưu tiên nguyện vọng thành công.");
        }

        public async Task<Result> SubmitRegistrationAsync(int registrationId, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);
            await EnsurePeriodOpenAsync(registration.MaDotDoAn, cancellationToken);

            registration.Submit();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registration {Id} submitted for approval", registrationId);
            return Result.Ok("Gửi đăng ký đề tài thành công. Chờ duyệt.");
        }

        public async Task<Result> CancelRegistrationAsync(int registrationId, CancellationToken cancellationToken = default)
        {
            var registration = await LoadForLeaderAsync(registrationId, cancellationToken);

            registration.Cancel();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registration {Id} cancelled", registrationId);
            return Result.Ok("Hủy đăng ký đề tài thành công.");
        }

        /// <summary>
        /// Duyệt một nguyện vọng: các nguyện vọng còn lại tự bị từ chối và cả đơn chuyển sang DA_DUYET.
        /// Điều kiện: số thành viên không vượt sức chứa đề tài, và đề tài chưa được giao cho nhóm khác trong đợt.
        /// </summary>
        public async Task<Result> ApprovePreferenceAsync(int preferenceId, CancellationToken cancellationToken = default)
        {
            var pref = await LoadPreferenceAsync(preferenceId, cancellationToken);
            var (topic, deciderId) = await GetTopicAndDeciderAsync(pref, cancellationToken);

            var memberCount = await _dbContext.ThanhVienDangKys.CountAsync(x => x.MaDangKy == pref.MaDangKy, cancellationToken);
            if (memberCount > topic.SoLuongSinhVienToiDa)
                throw new BusinessRuleException($"Nhóm có {memberCount} thành viên, vượt sức chứa đề tài ({topic.SoLuongSinhVienToiDa}).");

            var alreadyTaken = await _dbContext.ChiTietDangKys.AnyAsync(x =>
                x.MaDeTaiDotDoAn == pref.MaDeTaiDotDoAn &&
                x.TrangThai == PreferenceStatus.DUOC_CHON &&
                x.DangKyDeTai.TrangThai == RegistrationStatus.DA_DUYET, cancellationToken);
            if (alreadyTaken)
                throw new BusinessRuleException("Đề tài này đã được giao cho một nhóm khác trong đợt.");

            pref.DangKyDeTai.Approve();
            pref.Approve(deciderId);
            foreach (var other in pref.DangKyDeTai.ChiTietDangKys.Where(x => x.Id != preferenceId && x.TrangThai == PreferenceStatus.CHO_DUYET))
            {
                other.Reject(deciderId, "Đã duyệt nguyện vọng khác của nhóm.");
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Preference {PrefId} approved by account {AccountId}", preferenceId, _actor.AccountId);

            return Result.Ok("Duyệt đề tài cho nhóm đăng ký thành công.");
        }

        /// <summary>Khi mọi nguyện vọng đều bị từ chối thì cả đơn chuyển sang TU_CHOI.</summary>
        public async Task<Result> RejectPreferenceAsync(int preferenceId, RejectPreferenceRequest request, CancellationToken cancellationToken = default)
        {
            var pref = await LoadPreferenceAsync(preferenceId, cancellationToken);
            var (_, deciderId) = await GetTopicAndDeciderAsync(pref, cancellationToken);
            if (pref.TrangThai != PreferenceStatus.CHO_DUYET)
                throw new BusinessRuleException("Nguyện vọng này đã được xử lý.");

            pref.Reject(deciderId, request.Reason);
            if (pref.DangKyDeTai.ChiTietDangKys.All(x => x.TrangThai == PreferenceStatus.TU_CHOI))
                pref.DangKyDeTai.Reject();

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Preference {PrefId} rejected by account {AccountId}: {Reason}", preferenceId, _actor.AccountId, request.Reason);

            return Result.Ok("Từ chối nguyện vọng đăng ký thành công.");
        }

        /// <summary>Nạp đơn để sửa: chỉ Admin hoặc trưởng nhóm của đơn.</summary>
        private async Task<DangKyDeTai> LoadForLeaderAsync(int registrationId, CancellationToken cancellationToken)
        {
            var registration = await _dbContext.DangKyDeTais
                .Include(x => x.ThanhViens)
                .Include(x => x.ChiTietDangKys)
                .FirstOrDefaultAsync(x => x.Id == registrationId, cancellationToken)
                ?? throw new NotFoundException("Đăng ký đề tài", registrationId);

            if (_actor.IsAdmin) return registration;

            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            if (!registration.ThanhViens.Any(m => m.MaSV == studentId && m.VaiTro == GroupMemberRole.TRUONG_NHOM))
                throw new ForbiddenException("Chỉ trưởng nhóm mới được thay đổi đơn đăng ký.");

            return registration;
        }

        private async Task<ChiTietDangKy> LoadPreferenceAsync(int preferenceId, CancellationToken cancellationToken)
        {
            var pref = await _dbContext.ChiTietDangKys
                .Include(x => x.DangKyDeTai).ThenInclude(d => d.ChiTietDangKys)
                .FirstOrDefaultAsync(x => x.Id == preferenceId, cancellationToken)
                ?? throw new NotFoundException("Nguyện vọng đăng ký", preferenceId);

            if (pref.DangKyDeTai.TrangThai != RegistrationStatus.CHO_DUYET)
                throw new BusinessRuleException("Đơn đăng ký chưa được gửi hoặc đã kết thúc xử lý.");
            return pref;
        }

        /// <summary>
        /// Người xử lý nguyện vọng: Admin (ghi nhận null) hoặc chính giảng viên đã đề xuất đề tài đó.
        /// </summary>
        private async Task<(DeTai Topic, int? DeciderId)> GetTopicAndDeciderAsync(ChiTietDangKy pref, CancellationToken cancellationToken)
        {
            var topic = await _dbContext.DeTaiDotDoAns.AsNoTracking()
                .Where(x => x.Id == pref.MaDeTaiDotDoAn)
                .Select(x => x.DeTai)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Đề tài trong đợt đồ án", pref.MaDeTaiDotDoAn);

            if (_actor.IsAdmin) return (topic, null);

            var lecturerId = await _actor.GetLecturerIdAsync(cancellationToken);
            if (topic.MaGVDeXuat != lecturerId)
                throw new ForbiddenException("Chỉ giảng viên ra đề tài này mới được duyệt / từ chối nguyện vọng.");
            return (topic, lecturerId);
        }

        private async Task EnsurePeriodOpenAsync(int periodId, CancellationToken cancellationToken)
        {
            var period = await _dbContext.DotDoAns.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == periodId, cancellationToken)
                ?? throw new NotFoundException("Đợt đồ án", periodId);

            if (period.TrangThai != PeriodStatus.DANG_MO_DANG_KY)
                throw new BusinessRuleException("Đợt đồ án hiện không mở đăng ký.");
            if (DateTime.UtcNow.Date > period.HanDangKyDeTai.Date)
                throw new BusinessRuleException($"Đã quá hạn đăng ký đề tài ({period.HanDangKyDeTai:dd/MM/yyyy}).");
        }

        /// <summary>
        /// SV phải tồn tại, thuộc lớp tham gia đợt, thỏa điều kiện tiên quyết của loại đồ án
        /// (chuyên ngành: đã đạt cơ sở), và (quy tắc 2) chưa thuộc nhóm nào khác chưa hủy trong cùng đợt.
        /// </summary>
        private async Task EnsureStudentEligibleAsync(int studentId, int periodId, CancellationToken cancellationToken)
        {
            var classId = await _dbContext.SinhViens
                .Where(x => x.Id == studentId)
                .Select(x => (int?)x.MaLop)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Sinh viên", studentId);

            if (!await _dbContext.DotDoAnLops.AnyAsync(x => x.MaDotDoAn == periodId && x.MaLop == classId, cancellationToken))
                throw new BusinessRuleException($"Lớp của sinh viên ID {studentId} không tham gia đợt đồ án này.");

            var policy = await _policies.ForPeriodAsync(_dbContext, periodId, cancellationToken);
            await policy.EnsureStudentEligibleAsync(studentId, cancellationToken);

            var alreadyRegistered = await _dbContext.ThanhVienDangKys.AnyAsync(x =>
                x.MaSV == studentId &&
                x.DangKyDeTai.MaDotDoAn == periodId &&
                x.DangKyDeTai.TrangThai != RegistrationStatus.DA_HUY, cancellationToken);
            if (alreadyRegistered)
                throw new BusinessRuleException($"Sinh viên ID {studentId} đã có nhóm đăng ký trong đợt đồ án này.");
        }

        private static RegistrationStatus ParseRegistrationStatus(string status)
        {
            if (Enum.TryParse<RegistrationStatus>(status, true, out var result))
                return result;
            throw new ValidationException("Status", $"Trạng thái '{status}' không hợp lệ.");
        }

        private static RegistrationDto MapToDto(DangKyDeTai x)
        {
            var members = x.ThanhViens.Select(m => new RegistrationMemberDto(m.Id, m.MaSV, m.VaiTro.ToString(), m.NgayThamGia)).ToList();
            var prefs = x.ChiTietDangKys.OrderBy(p => p.ThuTuUuTien).Select(p => new RegistrationPreferenceDto(
                p.Id, p.MaDeTaiDotDoAn, p.ThuTuUuTien, p.TrangThai.ToString(), p.MaGVDuyet, p.NgayDuyet, p.LyDoTuChoi)).ToList();
            return new RegistrationDto(x.Id, x.MaDotDoAn, x.TenNhom, x.NgayDangKy, x.TrangThai.ToString(), members, prefs);
        }
    }
}
