using Microsoft.EntityFrameworkCore;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Topic;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.Registration
{
    public interface IRegistrationChangeService
    {
        Task<ChangeRequestDto> CreateAsync(CreateChangeRequestRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChangeRequestDto>> GetAsync(int? periodId, string? status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChangeRequestDto>> GetMineAsync(CancellationToken cancellationToken = default);
        Task<Result> ApproveAsync(int id, ApproveChangeRequestRequest request, CancellationToken cancellationToken = default);
        Task<Result> RejectAsync(int id, RejectChangeRequestRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Yêu cầu đổi đề tài / đổi nhóm sau khi đơn đăng ký đã duyệt.
    /// - DOI_DE_TAI: trưởng nhóm gửi; khi duyệt, nguyện vọng được chọn chuyển sang đề tài mới và đồ án (nếu đã tạo) đổi đề tài.
    /// - DOI_NHOM: một sinh viên trong nhóm xin chuyển sang nhóm đã duyệt khác cùng đợt; khi duyệt, chuyển thành viên ở cả
    ///   đơn đăng ký và đồ án (nếu đã tạo).
    /// Khi duyệt vẫn kiểm tra lại các quy tắc hiện có: quy mô nhóm của loại đồ án, sức chứa đề tài, đề tài chưa giao cho nhóm khác,
    /// một sinh viên một nhóm trong đợt. Không cho đổi khi kết quả của đồ án liên quan đã công bố.
    /// </summary>
    public class RegistrationChangeService : IRegistrationChangeService
    {
        private readonly AppDbContext _dbContext;
        private readonly CurrentActor _actor;
        private readonly ProjectTypePolicyResolver _policies;
        private readonly ILogger<RegistrationChangeService> _logger;

        public RegistrationChangeService(AppDbContext dbContext, CurrentActor actor, ProjectTypePolicyResolver policies, ILogger<RegistrationChangeService> logger)
        {
            _dbContext = dbContext;
            _actor = actor;
            _policies = policies;
            _logger = logger;
        }

        public async Task<ChangeRequestDto> CreateAsync(CreateChangeRequestRequest request, CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            if (!Enum.TryParse<ChangeRequestType>(request.Type, true, out var type) || !Enum.IsDefined(type))
                throw new ValidationException("Type", $"Loại yêu cầu '{request.Type}' không hợp lệ. Hợp lệ: {string.Join(", ", Enum.GetNames<ChangeRequestType>())}.");

            var registration = await LoadRegistrationAsync(request.RegistrationId, cancellationToken);
            var member = registration.ThanhViens.FirstOrDefault(m => m.MaSV == studentId)
                ?? throw new ForbiddenException("Bạn không thuộc nhóm đăng ký này.");
            if (type == ChangeRequestType.DOI_DE_TAI && member.VaiTro != GroupMemberRole.TRUONG_NHOM)
                throw new ForbiddenException("Chỉ trưởng nhóm mới được yêu cầu đổi đề tài.");
            if (registration.TrangThai != RegistrationStatus.DA_DUYET)
                throw new BusinessRuleException("Chỉ gửi yêu cầu thay đổi khi đơn đăng ký đã được duyệt.");

            if (await _dbContext.YeuCauThayDois.AnyAsync(x => x.MaSVYeuCau == studentId && x.TrangThai == ChangeRequestStatus.CHO_DUYET, cancellationToken))
                throw new BusinessRuleException("Bạn đang có một yêu cầu thay đổi chờ duyệt.");

            var entity = new YeuCauThayDoi(registration.Id, studentId, type, request.NewPeriodTopicId, request.TargetRegistrationId, request.Reason);

            // Kiểm tra sớm để sinh viên biết ngay; khi duyệt sẽ kiểm tra lại vì dữ liệu có thể đã đổi.
            if (type == ChangeRequestType.DOI_DE_TAI)
                await ValidateTopicChangeAsync(registration, request.NewPeriodTopicId!.Value, cancellationToken);
            else
                await ValidateGroupChangeAsync(registration, studentId, await LoadRegistrationAsync(request.TargetRegistrationId!.Value, cancellationToken), cancellationToken);

            _dbContext.YeuCauThayDois.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Change request {Id} ({Type}) created by student {StudentId} for registration {RegId}", entity.Id, type, studentId, registration.Id);
            return MapToDto(entity);
        }

        public async Task<IReadOnlyList<ChangeRequestDto>> GetAsync(int? periodId, string? status, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.YeuCauThayDois.AsNoTracking().AsQueryable();
            if (periodId.HasValue)
                query = query.Where(x => _dbContext.DangKyDeTais.Any(d => d.Id == x.MaDangKy && d.MaDotDoAn == periodId.Value));
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ChangeRequestStatus>(status, true, out var parsed))
                    throw new ValidationException("Status", $"Trạng thái '{status}' không hợp lệ.");
                query = query.Where(x => x.TrangThai == parsed);
            }
            var list = await query.OrderByDescending(x => x.NgayYeuCau).ToListAsync(cancellationToken);
            return list.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<ChangeRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            var list = await _dbContext.YeuCauThayDois.AsNoTracking()
                .Where(x => x.MaSVYeuCau == studentId)
                .OrderByDescending(x => x.NgayYeuCau)
                .ToListAsync(cancellationToken);
            return list.Select(MapToDto).ToList();
        }

        public async Task<Result> RejectAsync(int id, RejectChangeRequestRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.YeuCauThayDois.FindAsync(new object[] { id }, cancellationToken)
                ?? throw new NotFoundException("Yêu cầu thay đổi", id);
            entity.Reject(_actor.AccountId, request.Reason);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Từ chối yêu cầu thay đổi thành công.");
        }

        /// <summary>
        /// Duyệt và áp dụng thay đổi. Chia hai lần SaveChanges trong một transaction: bước 1 gỡ dữ liệu cũ (bỏ chọn nguyện vọng /
        /// rút thành viên), bước 2 ghi dữ liệu mới, để không vi phạm các index unique có điều kiện (một nguyện vọng DUOC_CHON,
        /// một trưởng nhóm) do SQL Server kiểm tra theo từng câu lệnh.
        /// </summary>
        public async Task<Result> ApproveAsync(int id, ApproveChangeRequestRequest request, CancellationToken cancellationToken = default)
        {
            var accountId = _actor.AccountId;
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var entity = await _dbContext.YeuCauThayDois.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Yêu cầu thay đổi", id);
                if (entity.TrangThai != ChangeRequestStatus.CHO_DUYET)
                    throw new BusinessRuleException("Yêu cầu này đã được xử lý.");

                var registration = await LoadRegistrationAsync(entity.MaDangKy, cancellationToken);
                if (entity.LoaiYeuCau == ChangeRequestType.DOI_DE_TAI)
                    await ApplyTopicChangeAsync(entity, registration, cancellationToken);
                else
                    await ApplyGroupChangeAsync(entity, registration, cancellationToken);

                entity.Approve(accountId, request.Note);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });

            _logger.LogInformation("Change request {Id} approved by account {AccountId}", id, accountId);
            return Result.Ok("Duyệt yêu cầu thay đổi thành công.");
        }

        // ---------------- Đổi đề tài ----------------

        private async Task<(DeTaiDotDoAn NewPeriodTopic, List<DoAn> Projects)> ValidateTopicChangeAsync(
            DangKyDeTai registration, int newPeriodTopicId, CancellationToken cancellationToken)
        {
            if (registration.TrangThai != RegistrationStatus.DA_DUYET)
                throw new BusinessRuleException("Đơn đăng ký không còn ở trạng thái đã duyệt.");

            var periodTopic = await _dbContext.DeTaiDotDoAns.Include(x => x.DeTai)
                .FirstOrDefaultAsync(x => x.Id == newPeriodTopicId, cancellationToken)
                ?? throw new NotFoundException("Đề tài trong đợt đồ án", newPeriodTopicId);
            if (periodTopic.MaDotDoAn != registration.MaDotDoAn)
                throw new BusinessRuleException("Đề tài mới không thuộc đợt đồ án của đơn đăng ký.");
            if (periodTopic.TrangThai != PeriodTopicStatus.DANG_MO)
                throw new BusinessRuleException("Đề tài mới hiện không mở đăng ký.");
            if (registration.SelectedPreference?.MaDeTaiDotDoAn == newPeriodTopicId)
                throw new BusinessRuleException("Nhóm đang làm đề tài này.");

            // Sức chứa đề tài
            if (registration.ThanhViens.Count > periodTopic.DeTai.SoLuongSinhVienToiDa)
                throw new BusinessRuleException($"Nhóm có {registration.ThanhViens.Count} thành viên, vượt sức chứa đề tài ({periodTopic.DeTai.SoLuongSinhVienToiDa}).");

            // Đề tài chưa giao cho nhóm / đồ án khác trong đợt
            var takenByRegistration = await _dbContext.ChiTietDangKys.AnyAsync(x =>
                x.MaDeTaiDotDoAn == newPeriodTopicId &&
                x.MaDangKy != registration.Id &&
                x.TrangThai == PreferenceStatus.DUOC_CHON &&
                x.DangKyDeTai.TrangThai == RegistrationStatus.DA_DUYET, cancellationToken);
            var takenByProject = await _dbContext.DoAns.AnyAsync(x =>
                x.MaDotDoAn == registration.MaDotDoAn &&
                x.MaDeTai == periodTopic.MaDeTai &&
                x.MaDangKy != registration.Id &&
                x.TrangThai != ProjectStatus.DA_HUY, cancellationToken);
            if (takenByRegistration || takenByProject)
                throw new BusinessRuleException("Đề tài này đã được giao cho một nhóm khác trong đợt.");

            var projects = await LoadProjectsOfRegistrationAsync(registration.Id, cancellationToken);
            await EnsureNotPublishedAsync(projects.Select(p => p.Id), cancellationToken);
            return (periodTopic, projects);
        }

        private async Task ApplyTopicChangeAsync(YeuCauThayDoi request, DangKyDeTai registration, CancellationToken cancellationToken)
        {
            var (periodTopic, projects) = await ValidateTopicChangeAsync(registration, request.MaDeTaiDotDoAnMoi!.Value, cancellationToken);

            registration.ReleaseSelectedTopic($"Đổi đề tài theo yêu cầu #{request.Id}.");
            await _dbContext.SaveChangesAsync(cancellationToken);

            registration.SelectTopic(periodTopic.Id, null);
            foreach (var project in projects)
                project.ChangeTopic(periodTopic.MaDeTai);
        }

        // ---------------- Đổi nhóm ----------------

        private async Task<(List<DoAn> SourceProjects, List<DoAn> TargetProjects, int MaxMembers)> ValidateGroupChangeAsync(
            DangKyDeTai source, int studentId, DangKyDeTai target, CancellationToken cancellationToken)
        {
            if (source.TrangThai != RegistrationStatus.DA_DUYET || target.TrangThai != RegistrationStatus.DA_DUYET)
                throw new BusinessRuleException("Nhóm hiện tại và nhóm mới đều phải là đơn đăng ký đã duyệt.");
            if (source.MaDotDoAn != target.MaDotDoAn)
                throw new BusinessRuleException("Chỉ được chuyển sang nhóm cùng đợt đồ án.");
            if (source.ThanhViens.All(m => m.MaSV != studentId))
                throw new BusinessRuleException("Sinh viên không còn thuộc nhóm hiện tại.");
            if (target.ThanhViens.Any(m => m.MaSV == studentId))
                throw new BusinessRuleException("Sinh viên đã thuộc nhóm mới.");

            // Quy mô nhóm của loại đồ án và sức chứa đề tài của nhóm mới
            var policy = await _policies.ForPeriodAsync(_dbContext, target.MaDotDoAn, cancellationToken);
            var maxMembers = policy.MaxMembers;
            var targetPeriodTopicId = target.SelectedPreference?.MaDeTaiDotDoAn;
            if (targetPeriodTopicId.HasValue)
            {
                var capacity = await _dbContext.DeTaiDotDoAns.Where(x => x.Id == targetPeriodTopicId.Value)
                    .Select(x => x.DeTai.SoLuongSinhVienToiDa).FirstAsync(cancellationToken);
                maxMembers = Math.Min(maxMembers, capacity);
            }
            if (target.ThanhViens.Count + 1 > maxMembers)
                throw new BusinessRuleException($"Nhóm mới đã đủ {maxMembers} thành viên (quy mô {policy.DisplayName} / sức chứa đề tài).");

            var sourceProjects = await LoadProjectsOfRegistrationAsync(source.Id, cancellationToken);
            var targetProjects = await LoadProjectsOfRegistrationAsync(target.Id, cancellationToken);
            await EnsureNotPublishedAsync(sourceProjects.Concat(targetProjects).Select(p => p.Id), cancellationToken);

            // Điểm đã chấm gắn với đồ án cũ; chuyển nhóm khi đã có điểm sẽ làm lệch kết quả nên không cho phép.
            var sourceProjectIds = sourceProjects.Select(p => p.Id).ToList();
            if (await _dbContext.PhieuChams.AnyAsync(x => sourceProjectIds.Contains(x.MaDoAn) && x.MaSV == studentId, cancellationToken))
                throw new BusinessRuleException("Sinh viên đã có điểm ở đồ án hiện tại, không thể chuyển nhóm.");

            return (sourceProjects, targetProjects, maxMembers);
        }

        private async Task ApplyGroupChangeAsync(YeuCauThayDoi request, DangKyDeTai source, CancellationToken cancellationToken)
        {
            var target = await LoadRegistrationAsync(request.MaDangKyMoi!.Value, cancellationToken);
            var studentId = request.MaSVYeuCau;
            var (sourceProjects, targetProjects, maxMembers) = await ValidateGroupChangeAsync(source, studentId, target, cancellationToken);

            // Bước 1: rút khỏi nhóm / đồ án cũ
            source.TransferOut(studentId);
            foreach (var project in sourceProjects.Where(p => p.ThanhViens.Any(m => m.MaSV == studentId)))
                project.RemoveMember(studentId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Bước 2: bổ sung trưởng nhóm nếu cần, thêm vào nhóm / đồ án mới
            source.EnsureLeader();
            foreach (var project in sourceProjects)
                project.EnsureLeader();
            target.TransferIn(studentId, maxMembers);
            foreach (var project in targetProjects)
                project.AddMember(studentId, ProjectMemberRole.THANH_VIEN, maxMembers);
        }

        // ---------------- Tiện ích ----------------

        private async Task<DangKyDeTai> LoadRegistrationAsync(int id, CancellationToken cancellationToken) =>
            await _dbContext.DangKyDeTais
                .Include(x => x.ThanhViens)
                .Include(x => x.ChiTietDangKys)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Đăng ký đề tài", id);

        private Task<List<DoAn>> LoadProjectsOfRegistrationAsync(int registrationId, CancellationToken cancellationToken) =>
            _dbContext.DoAns
                .Include(x => x.ThanhViens)
                .Where(x => x.MaDangKy == registrationId && x.TrangThai != ProjectStatus.DA_HUY)
                .ToListAsync(cancellationToken);

        private async Task EnsureNotPublishedAsync(IEnumerable<int> projectIds, CancellationToken cancellationToken)
        {
            var ids = projectIds.ToList();
            if (ids.Count > 0 && await _dbContext.KetQuaDoAns.AnyAsync(x => ids.Contains(x.MaDoAn) && x.NgayCongBo != null, cancellationToken))
                throw new BusinessRuleException("Kết quả đồ án đã công bố, không thể đổi đề tài hoặc đổi nhóm.");
        }

        private static ChangeRequestDto MapToDto(YeuCauThayDoi x) => new(
            x.Id, x.MaDangKy, x.MaSVYeuCau, x.LoaiYeuCau.ToString(), x.MaDeTaiDotDoAnMoi, x.MaDangKyMoi,
            x.LyDo, x.TrangThai.ToString(), x.NgayYeuCau, x.NgayXuLy, x.GhiChuXuLy);
    }
}
