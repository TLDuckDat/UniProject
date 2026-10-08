using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Data;

namespace UniProject.API.Modules.ProjectPeriod
{
    public class ProjectPeriodService : IProjectPeriodService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ProjectPeriodService> _logger;

        private readonly ProjectTypePolicyResolver _policies;

        public ProjectPeriodService(AppDbContext dbContext, ILogger<ProjectPeriodService> logger, ProjectTypePolicyResolver policies)
        {
            _dbContext = dbContext;
            _logger = logger;
            _policies = policies;
        }

        public Task<IReadOnlyList<ProjectTypeDto>> GetProjectTypesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProjectTypeDto> types = _policies.All
                .OrderBy(p => p.Kind)
                .Select(p => new ProjectTypeDto(p.Kind.ToString(), p.DisplayName, p.MaxMembers, p.RequiresReviewer,
                    p.Weights.Advisor, p.Weights.Reviewer, p.Weights.Committee,
                    p.PassRules.FinalPassMark, p.PassRules.ComponentPassMark, p.GradingMode.ToString()))
                .ToList();
            return Task.FromResult(types);
        }

        public async Task<PagedResult<ProjectPeriodDto>> GetProjectPeriodsAsync(
            PaginationParams @params,
            int? semesterId = null,
            string? status = null,
            string? projectKind = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.DotDoAns
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(projectKind))
            {
                var kind = ProjectTypePolicyResolver.Parse(projectKind);
                query = query.Where(x => x.LoaiDoAn == kind);
            }

            if (semesterId.HasValue)
                query = query.Where(x => x.MaHocKy == semesterId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = ParseStatus(status);
                query = query.Where(x => x.TrangThai == parsed);
            }

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaDotDoAnCode.ToLower().Contains(s) || x.TenDotDoAn.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.NgayBatDau)
                : query.OrderBy(x => x.NgayBatDau);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => MapToDto(x),
                cancellationToken);
        }

        public async Task<ProjectPeriodDto> GetProjectPeriodByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.DotDoAns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (item == null) throw new NotFoundException("Đợt đồ án", id);
            return MapToDto(item);
        }

        public async Task<ProjectPeriodDto> CreateProjectPeriodAsync(CreateProjectPeriodRequest request, CancellationToken cancellationToken = default)
        {
            ValidateDates(request.StartDate, request.EndDate, request.DefenseStartDate, request.DefenseEndDate);

            if (await _dbContext.DotDoAns.AnyAsync(x => x.MaDotDoAnCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã đợt đồ án '{request.Code}' đã tồn tại.");

            var kind = ProjectTypePolicyResolver.Parse(request.ProjectKind);
            await EnsureSemesterExistsAsync(request.SemesterId, cancellationToken);

            var entity = new DotDoAn(
                request.Code,
                request.Name,
                kind,
                request.SemesterId,
                request.StartDate,
                request.EndDate,
                request.RegistrationDeadline,
                request.OutlineDeadline,
                request.ReportDeadline,
                request.DefenseStartDate,
                request.DefenseEndDate);

            _dbContext.DotDoAns.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created project period {Code} - {Name}", entity.MaDotDoAnCode, entity.TenDotDoAn);
            return await GetProjectPeriodByIdAsync(entity.Id, cancellationToken);
        }

        public async Task<ProjectPeriodDto> UpdateProjectPeriodAsync(int id, UpdateProjectPeriodRequest request, CancellationToken cancellationToken = default)
        {
            ValidateDates(request.StartDate, request.EndDate, request.DefenseStartDate, request.DefenseEndDate);

            var entity = await _dbContext.DotDoAns.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đợt đồ án", id);

            await EnsureSemesterExistsAsync(request.SemesterId, cancellationToken);

            entity.Update(
                request.Name,
                request.SemesterId,
                request.StartDate,
                request.EndDate,
                request.RegistrationDeadline,
                request.OutlineDeadline,
                request.ReportDeadline,
                request.DefenseStartDate,
                request.DefenseEndDate);

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated project period ID {Id}", id);

            return await GetProjectPeriodByIdAsync(entity.Id, cancellationToken);
        }

        public async Task<Result> ChangeStatusAsync(int id, ChangePeriodStatusRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.DotDoAns.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Đợt đồ án", id);

            var status = ParseStatus(request.Status);
            entity.ChangeStatus(status);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Changed status of project period ID {Id} to {Status}", id, status);
            return Result.Ok($"Cập nhật trạng thái đợt đồ án thành '{request.Status}' thành công.");
        }

        public async Task<Result> AddClassToPeriodAsync(int periodId, int classId, CancellationToken cancellationToken = default)
        {
            if (!await _dbContext.DotDoAns.AnyAsync(x => x.Id == periodId, cancellationToken))
                throw new NotFoundException("Đợt đồ án", periodId);
            if (!await _dbContext.Lops.AnyAsync(x => x.Id == classId, cancellationToken))
                throw new NotFoundException("Lớp", classId);

            if (await _dbContext.DotDoAnLops.AnyAsync(x => x.MaDotDoAn == periodId && x.MaLop == classId, cancellationToken))
                throw new ConflictException("Lớp này đã được thêm vào đợt đồ án.");

            var item = new DotDoAnLop(periodId, classId);
            _dbContext.DotDoAnLops.Add(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Thêm lớp vào đợt đồ án thành công.");
        }

        public async Task<Result> RemoveClassFromPeriodAsync(int periodId, int classId, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.DotDoAnLops
                .FirstOrDefaultAsync(x => x.MaDotDoAn == periodId && x.MaLop == classId, cancellationToken);

            if (item == null) throw new NotFoundException($"Lớp ID {classId} không thuộc đợt đồ án ID {periodId}.");

            _dbContext.DotDoAnLops.Remove(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Xóa lớp khỏi đợt đồ án thành công.");
        }

        public async Task<IReadOnlyList<int>> GetClassesInPeriodAsync(int periodId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.DotDoAnLops
                .AsNoTracking()
                .Where(x => x.MaDotDoAn == periodId)
                .Select(x => x.MaLop)
                .ToListAsync(cancellationToken);
        }

        private static void ValidateDates(DateTime start, DateTime end, DateTime defStart, DateTime defEnd)
        {
            if (end < start)
                throw new ValidationException("EndDate", "Ngày kết thúc đợt đồ án phải lớn hơn hoặc bằng ngày bắt đầu.");
            if (defEnd < defStart)
                throw new ValidationException("DefenseEndDate", "Ngày kết thúc bảo vệ phải lớn hơn hoặc bằng ngày bắt đầu bảo vệ.");
        }

        private static PeriodStatus ParseStatus(string status)
        {
            if (Enum.TryParse<PeriodStatus>(status, true, out var result))
            {
                return result;
            }
            throw new ValidationException("Status", $"Trạng thái '{status}' không hợp lệ.");
        }

        private async Task EnsureSemesterExistsAsync(int semesterId, CancellationToken cancellationToken)
        {
            if (!await _dbContext.HocKys.AnyAsync(x => x.Id == semesterId, cancellationToken))
                throw new NotFoundException("Học kỳ", semesterId);
        }

        private static ProjectPeriodDto MapToDto(DotDoAn x)
        {
            return new ProjectPeriodDto(
                x.Id,
                x.MaDotDoAnCode,
                x.TenDotDoAn,
                x.LoaiDoAn.ToString(),
                x.LoaiDoAn.ToDisplayName(),
                x.MaHocKy,
                x.NgayBatDau,
                x.NgayKetThuc,
                x.HanDangKyDeTai,
                x.HanNopDeCuong,
                x.HanNopBaoCao,
                x.NgayBatDauBaoVe,
                x.NgayKetThucBaoVe,
                x.TrangThai.ToString(),
                x.IsActive);
        }
    }
}
