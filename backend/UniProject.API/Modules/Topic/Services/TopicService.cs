using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.ProjectPeriod;

namespace UniProject.API.Modules.Topic
{
    public class TopicService : ITopicService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TopicService> _logger;
        private readonly CurrentActor _actor;
        private readonly ProjectTypePolicyResolver _policies;

        public TopicService(AppDbContext dbContext, ILogger<TopicService> logger, CurrentActor actor, ProjectTypePolicyResolver policies)
        {
            _dbContext = dbContext;
            _logger = logger;
            _actor = actor;
            _policies = policies;
        }

        /// <summary>Số SV tối đa của đề tài không được vượt quy mô nhóm của loại đồ án (theo cấu hình ProjectTypes).</summary>
        private void EnsureCapacityFitsKind(int maxStudents, ProjectKind kind)
        {
            var policy = _policies.Get(kind);
            if (maxStudents > policy.MaxMembers)
                throw new ValidationException("MaxStudents", $"{policy.DisplayName} chỉ cho phép tối đa {policy.MaxMembers} sinh viên.");
        }

        /// <summary>Giảng viên chỉ được thao tác trên đề tài do chính mình đề xuất; Admin thao tác được mọi đề tài.</summary>
        private async Task EnsureCanEditAsync(DeTai topic, CancellationToken cancellationToken)
        {
            if (_actor.IsAdmin) return;
            if (topic.MaGVDeXuat != await _actor.GetLecturerIdAsync(cancellationToken))
                throw new ForbiddenException("Bạn chỉ được chỉnh sửa đề tài do mình đề xuất.");
        }

        /// <summary>
        /// Người duyệt: Admin (ghi nhận MaGVDuyet = null) hoặc giảng viên. Đề tài do giảng viên đề xuất thì người duyệt
        /// phải KHÁC người đề xuất; đề tài do sinh viên đề xuất thì chỉ giảng viên dự kiến hướng dẫn được duyệt.
        /// Trả về mã giảng viên duyệt để lưu vào đề tài.
        /// </summary>
        private async Task<int?> GetReviewerIdAsync(DeTai topic, CancellationToken cancellationToken)
        {
            if (_actor.IsAdmin) return null;
            var lecturerId = await _actor.GetLecturerIdAsync(cancellationToken);
            if (topic.IsStudentProposed)
            {
                if (topic.MaGVDeXuat != lecturerId)
                    throw new ForbiddenException("Đề tài do sinh viên đề xuất chỉ do giảng viên dự kiến hướng dẫn hoặc quản trị viên duyệt.");
                return lecturerId;
            }
            if (topic.MaGVDeXuat == lecturerId)
                throw new ForbiddenException("Không được tự duyệt / từ chối đề tài do chính mình đề xuất.");
            return lecturerId;
        }

        public async Task<PagedResult<TopicDto>> GetTopicsAsync(
            PaginationParams @params,
            int? lecturerId = null,
            int? majorId = null,
            string? status = null,
            string? projectKind = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.DeTais.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(projectKind))
            {
                var kind = ProjectTypePolicyResolver.Parse(projectKind);
                query = query.Where(x => x.LoaiDoAn == kind);
            }

            if (lecturerId.HasValue)
                query = query.Where(x => x.MaGVDeXuat == lecturerId.Value);

            if (majorId.HasValue)
                query = query.Where(x => x.MaChuyenNganh == majorId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = ParseStatus(status);
                query = query.Where(x => x.TrangThai == parsed);
            }

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaDeTaiCode.ToLower().Contains(s) || x.TenDeTai.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => MapToDto(x),
                cancellationToken);
        }

        public async Task<TopicDto> GetTopicByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTais.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài", id);
            return MapToDto(entity);
        }

        public async Task<TopicDto> CreateTopicAsync(CreateTopicRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.DeTais.AnyAsync(x => x.MaDeTaiCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã đề tài '{request.Code}' đã tồn tại.");

            int proposerId;
            if (_actor.IsAdmin)
            {
                proposerId = request.ProposedByLecturerId
                    ?? throw new ValidationException("ProposedByLecturerId", "Admin tạo đề tài phải chọn giảng viên đề xuất.");
                if (!await _dbContext.GiangViens.AnyAsync(x => x.Id == proposerId, cancellationToken))
                    throw new NotFoundException("Giảng viên", proposerId);
            }
            else
            {
                proposerId = await _actor.GetLecturerIdAsync(cancellationToken);
            }

            if (!await _dbContext.ChuyenNganhs.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
                throw new NotFoundException("Chuyên ngành", request.MajorId);

            var kind = ProjectTypePolicyResolver.Parse(request.ProjectKind);
            EnsureCapacityFitsKind(request.MaxStudents, kind);

            var entity = new DeTai(
                request.Code,
                request.Title,
                request.Description,
                request.Objectives,
                request.Requirements,
                request.ExpectedTechnologies,
                request.MaxStudents,
                proposerId,
                request.MajorId,
                kind);

            _dbContext.DeTais.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {Code} created by lecturer {LecturerId}", entity.MaDeTaiCode, entity.MaGVDeXuat);
            return MapToDto(entity);
        }

        /// <summary>
        /// Sinh viên đề xuất đề tài: đứng tên sinh viên đang đăng nhập, kèm giảng viên dự kiến hướng dẫn.
        /// Đề tài ở trạng thái CHO_DUYET, do giảng viên đó hoặc Admin duyệt.
        /// </summary>
        public async Task<TopicDto> ProposeTopicByStudentAsync(StudentProposeTopicRequest request, CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            if (await _dbContext.DeTais.AnyAsync(x => x.MaDeTaiCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã đề tài '{request.Code}' đã tồn tại.");
            if (!await _dbContext.GiangViens.AnyAsync(x => x.Id == request.ExpectedAdvisorLecturerId, cancellationToken))
                throw new NotFoundException("Giảng viên", request.ExpectedAdvisorLecturerId);
            if (!await _dbContext.ChuyenNganhs.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
                throw new NotFoundException("Chuyên ngành", request.MajorId);

            var kind = ProjectTypePolicyResolver.Parse(request.ProjectKind);
            EnsureCapacityFitsKind(request.MaxStudents, kind);

            var entity = new DeTai(
                request.Code,
                request.Title,
                request.Description,
                request.Objectives,
                request.Requirements,
                request.ExpectedTechnologies,
                request.MaxStudents,
                request.ExpectedAdvisorLecturerId,
                request.MajorId,
                kind,
                maSVDeXuat: studentId);

            _dbContext.DeTais.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {Code} proposed by student {StudentId}, expected advisor {LecturerId}",
                entity.MaDeTaiCode, studentId, entity.MaGVDeXuat);
            return MapToDto(entity);
        }

        public async Task<TopicDto> UpdateTopicAsync(int id, UpdateTopicRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTais.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài", id);
            await EnsureCanEditAsync(entity, cancellationToken);

            if (!await _dbContext.ChuyenNganhs.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
                throw new NotFoundException("Chuyên ngành", request.MajorId);
            EnsureCapacityFitsKind(request.MaxStudents, entity.LoaiDoAn);

            entity.Update(
                request.Title,
                request.Description,
                request.Objectives,
                request.Requirements,
                request.ExpectedTechnologies,
                request.MaxStudents,
                request.MajorId);

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Topic {Id} updated", id);

            return MapToDto(entity);
        }

        public async Task<Result> ApproveTopicAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTais.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài", id);

            var reviewerId = await GetReviewerIdAsync(entity, cancellationToken);
            entity.Approve(reviewerId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {Id} approved by account {AccountId}", id, _actor.AccountId);
            return Result.Ok("Duyệt đề tài thành công.");
        }

        public async Task<Result> RejectTopicAsync(int id, RejectTopicRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTais.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài", id);

            var reviewerId = await GetReviewerIdAsync(entity, cancellationToken);
            entity.Reject(reviewerId, request.Reason);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {Id} rejected by account {AccountId}: {Reason}", id, _actor.AccountId, request.Reason);
            return Result.Ok("Từ chối đề tài thành công.");
        }

        public async Task<Result> DisableTopicAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTais.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài", id);

            entity.Disable();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {Id} disabled", id);
            return Result.Ok("Ngừng sử dụng đề tài thành công.");
        }

        /// <summary>
        /// Quy tắc 3: chỉ đề tài đã duyệt (DA_DUYET) mới được mở trong đợt đồ án.
        /// </summary>
        public async Task<Result> AddTopicToPeriodAsync(AddTopicToPeriodRequest request, CancellationToken cancellationToken = default)
        {
            var topic = await _dbContext.DeTais.FindAsync(new object[] { request.TopicId }, cancellationToken);
            if (topic == null) throw new NotFoundException("Đề tài", request.TopicId);

            if (topic.TrangThai != TopicStatus.DA_DUYET)
            {
                throw new BusinessRuleException("Chỉ đề tài đã được duyệt (DA_DUYET) mới được thêm vào đợt đồ án.");
            }

            var periodKind = await _dbContext.DotDoAns.Where(x => x.Id == request.PeriodId)
                .Select(x => (ProjectKind?)x.LoaiDoAn).FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Đợt đồ án", request.PeriodId);
            if (periodKind != topic.LoaiDoAn)
                throw new BusinessRuleException(
                    $"Đề tài dành cho {topic.LoaiDoAn.ToDisplayName()} không thể mở trong đợt {periodKind.ToDisplayName()}.");

            if (await _dbContext.DeTaiDotDoAns.AnyAsync(x => x.MaDeTai == request.TopicId && x.MaDotDoAn == request.PeriodId, cancellationToken))
            {
                throw new ConflictException("Đề tài đã được thêm vào đợt đồ án này.");
            }

            var status = request.Status?.ToUpperInvariant() == "CHUA_MO" ? PeriodTopicStatus.CHUA_MO : PeriodTopicStatus.DANG_MO;
            var periodTopic = new DeTaiDotDoAn(request.TopicId, request.PeriodId, status);

            _dbContext.DeTaiDotDoAns.Add(periodTopic);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Topic {TopicId} added to period {PeriodId}", request.TopicId, request.PeriodId);
            return Result.Ok("Mở đề tài trong đợt đồ án thành công.");
        }

        public async Task<Result> ChangePeriodTopicStatusAsync(int periodTopicId, ChangePeriodTopicStatusRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DeTaiDotDoAns.FindAsync(new object[] { periodTopicId }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đề tài trong đợt đồ án", periodTopicId);

            if (!Enum.TryParse<PeriodTopicStatus>(request.Status, true, out var status))
            {
                throw new ValidationException("Status", $"Trạng thái '{request.Status}' không hợp lệ.");
            }

            entity.ChangeStatus(status);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Cập nhật trạng thái đề tài trong đợt đồ án thành công.");
        }

        public async Task<IReadOnlyList<PeriodTopicDto>> GetTopicsInPeriodAsync(int periodId, string? status = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.DeTaiDotDoAns
                .AsNoTracking()
                .Include(x => x.DeTai)
                .Where(x => x.MaDotDoAn == periodId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PeriodTopicStatus>(status, true, out var parsed))
            {
                query = query.Where(x => x.TrangThai == parsed);
            }

            return await query.Select(x => new PeriodTopicDto(
                x.Id,
                x.MaDeTai,
                x.DeTai.MaDeTaiCode,
                x.DeTai.TenDeTai,
                x.DeTai.SoLuongSinhVienToiDa,
                x.MaDotDoAn,
                x.DeTai.LoaiDoAn.ToString(),
                x.TrangThai.ToString())).ToListAsync(cancellationToken);
        }

        private static TopicStatus ParseStatus(string status)
        {
            if (Enum.TryParse<TopicStatus>(status, true, out var result))
            {
                return result;
            }
            throw new ValidationException("Status", $"Trạng thái '{status}' không hợp lệ.");
        }

        private static TopicDto MapToDto(DeTai x)
        {
            return new TopicDto(
                x.Id,
                x.MaDeTaiCode,
                x.TenDeTai,
                x.MoTa,
                x.MucTieu,
                x.YeuCau,
                x.CongNgheDuKien,
                x.SoLuongSinhVienToiDa,
                x.LoaiDoAn.ToString(),
                x.MaGVDeXuat,
                x.MaSVDeXuat,
                x.MaChuyenNganh,
                x.MaGVDuyet,
                x.NgayDuyet,
                x.LyDoTuChoi,
                x.TrangThai.ToString(),
                x.CreatedAt,
                x.IsActive);
        }
    }
}
