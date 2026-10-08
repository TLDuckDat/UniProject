using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Data;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;

namespace UniProject.API.Modules.Defense
{
    public class DefenseService : IDefenseService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DefenseService> _logger;
        private readonly ProjectTypePolicyResolver _policies;

        public DefenseService(AppDbContext dbContext, ILogger<DefenseService> logger, ProjectTypePolicyResolver policies)
        {
            _dbContext = dbContext;
            _logger = logger;
            _policies = policies;
        }

        public async Task<Result> AssignReviewerAsync(int projectId, AssignReviewerRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _dbContext.DoAns.AnyAsync(x => x.Id == projectId, cancellationToken))
                throw new NotFoundException("Đồ án", projectId);
            await EnsureLecturerExistsAsync(request.LecturerId, cancellationToken);

            var policy = await _policies.ForProjectAsync(_dbContext, projectId, cancellationToken);
            if (!policy.RequiresReviewer)
                throw new BusinessRuleException($"{policy.DisplayName} không có giảng viên phản biện.");

            // Phản biện phải độc lập: giảng viên đang hướng dẫn đồ án không được phản biện chính đồ án đó.
            if (await _dbContext.PhanCongHuongDans.AnyAsync(x => x.MaDoAn == projectId && x.MaGV == request.LecturerId && x.TrangThai, cancellationToken))
                throw new BusinessRuleException("Giảng viên hướng dẫn không được phản biện đồ án mình hướng dẫn.");

            if (await _dbContext.PhanBienDoAns.AnyAsync(x => x.MaDoAn == projectId && x.MaGV == request.LecturerId, cancellationToken))
                throw new ConflictException("Giảng viên này đã được phân công phản biện cho đồ án.");

            var assignment = new PhanBienDoAn(projectId, request.LecturerId);
            _dbContext.PhanBienDoAns.Add(assignment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned reviewer {LecturerId} to project {ProjectId}", request.LecturerId, projectId);
            return Result.Ok("Phân công giảng viên phản biện thành công.");
        }

        public async Task<IReadOnlyList<ReviewerAssignmentDto>> GetReviewersAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.PhanBienDoAns
                .AsNoTracking()
                .Where(x => x.MaDoAn == projectId)
                .Select(x => new ReviewerAssignmentDto(
                    x.Id,
                    x.MaDoAn,
                    x.MaGV,
                    x.NgayPhanCong,
                    x.TrangThai.ToString().ToUpperInvariant()))
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<CommitteeDto>> GetCommitteesAsync(PaginationParams @params, int? periodId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.HoiDongs
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .AsQueryable();

            if (periodId.HasValue)
                query = query.Where(x => x.MaDotDoAn == periodId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaHoiDongCode.ToLower().Contains(s) || x.TenHoiDong.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.NgayTao)
                : query.OrderBy(x => x.NgayTao);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => MapToCommitteeDto(x),
                cancellationToken);
        }

        public async Task<CommitteeDto> GetCommitteeByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.HoiDongs
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (item == null) throw new NotFoundException("Hội đồng", id);
            return MapToCommitteeDto(item);
        }

        public async Task<CommitteeDto> CreateCommitteeAsync(CreateCommitteeRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.HoiDongs.AnyAsync(x => x.MaHoiDongCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã hội đồng '{request.Code}' đã tồn tại.");
            if (!await _dbContext.DotDoAns.AnyAsync(x => x.Id == request.PeriodId, cancellationToken))
                throw new NotFoundException("Đợt đồ án", request.PeriodId);

            var entity = new HoiDong(request.Code, request.Name, request.PeriodId, request.Location);
            _dbContext.HoiDongs.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created committee {Code} - {Name}", entity.MaHoiDongCode, entity.TenHoiDong);
            return MapToCommitteeDto(entity);
        }

        public async Task<Result> AddCommitteeMemberAsync(int committeeId, AddCommitteeMemberRequest request, CancellationToken cancellationToken = default)
        {
            var committee = await _dbContext.HoiDongs
                .Include(x => x.ThanhViens)
                .FirstOrDefaultAsync(x => x.Id == committeeId, cancellationToken);

            if (committee == null) throw new NotFoundException("Hội đồng", committeeId);
            await EnsureLecturerExistsAsync(request.LecturerId, cancellationToken);

            var role = request.Role.ToUpperInvariant() switch
            {
                "CHU_TICH" => CommitteeRole.CHU_TICH,
                "THU_KY" => CommitteeRole.THU_KY,
                _ => CommitteeRole.UY_VIEN
            };

            committee.AddMember(request.LecturerId, role);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added lecturer {LecturerId} ({Role}) to committee {CommitteeId}", request.LecturerId, role, committeeId);
            return Result.Ok("Thêm thành viên hội đồng thành công.");
        }

        public async Task<Result> RemoveCommitteeMemberAsync(int committeeId, int memberId, CancellationToken cancellationToken = default)
        {
            var member = await _dbContext.ThanhVienHoiDongs
                .FirstOrDefaultAsync(x => x.Id == memberId && x.MaHoiDong == committeeId, cancellationToken);

            if (member == null) throw new NotFoundException("Thành viên hội đồng", memberId);

            // Hội đồng đã có lịch bảo vệ thì không được bớt thành viên làm hội đồng mất điều kiện bảo vệ.
            var hasActiveSchedule = await _dbContext.LichBaoVes
                .AnyAsync(x => x.MaHoiDong == committeeId && x.TrangThai != ScheduleStatus.DA_HUY, cancellationToken);
            if (hasActiveSchedule)
            {
                var remainingRoles = await _dbContext.ThanhVienHoiDongs
                    .Where(x => x.MaHoiDong == committeeId && x.Id != memberId)
                    .Select(x => x.VaiTro)
                    .ToListAsync(cancellationToken);
                var errors = HoiDong.GetCompositionErrors(remainingRoles);
                if (errors.Count > 0)
                    throw new BusinessRuleException("Hội đồng đã có lịch bảo vệ, không thể xóa thành viên này: " + string.Join(" ", errors));
            }

            _dbContext.ThanhVienHoiDongs.Remove(member);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok("Xóa thành viên khỏi hội đồng thành công.");
        }

        public async Task<DefenseScheduleDto> CreateDefenseScheduleAsync(CreateDefenseScheduleRequest request, CancellationToken cancellationToken = default)
        {
            if (request.EndTime <= request.StartTime)
                throw new ValidationException("EndTime", "Giờ kết thúc phải sau giờ bắt đầu.");

            var project = await _dbContext.DoAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Đồ án", request.ProjectId);
            var committee = await _dbContext.HoiDongs.AsNoTracking().Include(x => x.ThanhViens)
                .FirstOrDefaultAsync(x => x.Id == request.CommitteeId, cancellationToken)
                ?? throw new NotFoundException("Hội đồng", request.CommitteeId);
            committee.EnsureReadyForDefense();
            if (project.MaDotDoAn != request.PeriodId || committee.MaDotDoAn != request.PeriodId)
                throw new BusinessRuleException("Đồ án, hội đồng và lịch bảo vệ phải cùng một đợt đồ án.");
            if (project.TrangThai is ProjectStatus.DA_HUY or ProjectStatus.TAM_DUNG)
                throw new BusinessRuleException("Đồ án đã hủy hoặc tạm dừng, không thể xếp lịch bảo vệ.");
            await EnsureWithinDefenseWindowAsync(request.PeriodId, request.DefenseDate, cancellationToken);

            await ValidateCollisionAsync(0, request.ProjectId, request.Room, request.CommitteeId, request.DefenseDate, request.StartTime, request.EndTime, cancellationToken);

            var schedule = new LichBaoVe(
                request.ProjectId,
                request.CommitteeId,
                request.PeriodId,
                request.DefenseDate,
                request.StartTime,
                request.EndTime,
                request.Room);

            _dbContext.LichBaoVes.Add(schedule);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Defense schedule created for project {ProjectId} on {Date} {Start}-{End} Room {Room}",
                request.ProjectId, request.DefenseDate.ToShortDateString(), request.StartTime, request.EndTime, request.Room);

            return MapToScheduleDto(schedule);
        }

        public async Task<DefenseScheduleDto> UpdateDefenseScheduleAsync(int id, UpdateDefenseScheduleRequest request, CancellationToken cancellationToken = default)
        {
            if (request.EndTime <= request.StartTime)
                throw new ValidationException("EndTime", "Giờ kết thúc phải sau giờ bắt đầu.");

            var schedule = await _dbContext.LichBaoVes.FindAsync(new object[] { id }, cancellationToken);
            if (schedule == null) throw new NotFoundException("Lịch bảo vệ", id);
            if (schedule.TrangThai == ScheduleStatus.DA_HUY)
                throw new BusinessRuleException("Lịch bảo vệ đã bị hủy.");
            await EnsureWithinDefenseWindowAsync(schedule.MaDotDoAn, request.DefenseDate, cancellationToken);
            var committee = await _dbContext.HoiDongs.AsNoTracking().Include(x => x.ThanhViens)
                .FirstAsync(x => x.Id == schedule.MaHoiDong, cancellationToken);
            committee.EnsureReadyForDefense();

            await ValidateCollisionAsync(id, schedule.MaDoAn, request.Room, schedule.MaHoiDong, request.DefenseDate, request.StartTime, request.EndTime, cancellationToken);

            schedule.Update(request.DefenseDate, request.StartTime, request.EndTime, request.Room);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Defense schedule ID {Id} updated", id);
            return MapToScheduleDto(schedule);
        }

        /// <summary>
        /// Xóa mềm: chỉ chuyển trạng thái DA_HUY để giữ lịch sử.
        /// </summary>
        public async Task<Result> DeleteDefenseScheduleAsync(int id, CancellationToken cancellationToken = default)
        {
            var schedule = await _dbContext.LichBaoVes.FindAsync(new object[] { id }, cancellationToken);
            if (schedule == null) throw new NotFoundException("Lịch bảo vệ", id);
            if (schedule.TrangThai == ScheduleStatus.DA_HUY)
                throw new BusinessRuleException("Lịch bảo vệ đã bị hủy trước đó.");

            schedule.ChangeStatus(ScheduleStatus.DA_HUY);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Defense schedule ID {Id} cancelled", id);
            return Result.Ok("Hủy lịch bảo vệ thành công.");
        }

        public async Task<IReadOnlyList<DefenseScheduleDto>> GetDefenseSchedulesAsync(
            int? periodId = null,
            int? committeeId = null,
            int? projectId = null,
            DateTime? date = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.LichBaoVes
                .AsNoTracking()
                .Where(x => x.TrangThai != ScheduleStatus.DA_HUY);

            if (periodId.HasValue) query = query.Where(x => x.MaDotDoAn == periodId.Value);
            if (committeeId.HasValue) query = query.Where(x => x.MaHoiDong == committeeId.Value);
            if (projectId.HasValue) query = query.Where(x => x.MaDoAn == projectId.Value);
            if (date.HasValue) query = query.Where(x => x.NgayBaoVe == date.Value.Date);

            return await query
                .OrderBy(x => x.NgayBaoVe).ThenBy(x => x.GioBatDau)
                .Select(x => MapToScheduleDto(x))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Kiểm tra trùng lịch (bỏ qua lịch đã hủy và lịch có Id = excludeId):
        /// - Quy tắc 10: cùng phòng, cùng ngày, khung giờ giao nhau.
        /// - Quy tắc 11: cùng hội đồng, cùng ngày, khung giờ giao nhau.
        /// - Quy tắc 12: một đồ án chỉ có một lịch bảo vệ.
        /// Hai khung giờ giao nhau khi giờ bắt đầu mới trước giờ kết thúc cũ VÀ giờ kết thúc mới sau giờ bắt đầu cũ.
        /// </summary>
        private async Task ValidateCollisionAsync(
            int excludeId,
            int projectId,
            string room,
            int committeeId,
            DateTime date,
            TimeSpan start,
            TimeSpan end,
            CancellationToken cancellationToken)
        {
            var checkDate = date.Date;

            var roomConflict = await _dbContext.LichBaoVes
                .AnyAsync(s => s.Id != excludeId &&
                               s.TrangThai != ScheduleStatus.DA_HUY &&
                               s.Phong == room &&
                               s.NgayBaoVe == checkDate &&
                               start < s.GioKetThuc && end > s.GioBatDau,
                          cancellationToken);

            if (roomConflict)
            {
                throw new ConflictException($"Phòng '{room}' đã có lịch bảo vệ trùng trong khoảng thời gian {start} - {end} ngày {checkDate:dd/MM/yyyy}.");
            }

            var committeeConflict = await _dbContext.LichBaoVes
                .AnyAsync(s => s.Id != excludeId &&
                               s.TrangThai != ScheduleStatus.DA_HUY &&
                               s.MaHoiDong == committeeId &&
                               s.NgayBaoVe == checkDate &&
                               start < s.GioKetThuc && end > s.GioBatDau,
                          cancellationToken);

            if (committeeConflict)
            {
                throw new ConflictException($"Hội đồng ID {committeeId} đã có lịch bảo vệ trùng trong khoảng thời gian {start} - {end} ngày {checkDate:dd/MM/yyyy}.");
            }

            if (projectId > 0)
            {
                var projectConflict = await _dbContext.LichBaoVes
                    .AnyAsync(s => s.Id != excludeId &&
                                   s.TrangThai != ScheduleStatus.DA_HUY &&
                                   s.MaDoAn == projectId,
                              cancellationToken);

                if (projectConflict)
                {
                    throw new ConflictException($"Đồ án ID {projectId} đã có lịch bảo vệ được phân công.");
                }
            }
        }

        private async Task EnsureLecturerExistsAsync(int lecturerId, CancellationToken cancellationToken)
        {
            if (!await _dbContext.GiangViens.AnyAsync(x => x.Id == lecturerId, cancellationToken))
                throw new NotFoundException("Giảng viên", lecturerId);
        }

        private async Task EnsureWithinDefenseWindowAsync(int periodId, DateTime date, CancellationToken cancellationToken)
        {
            var period = await _dbContext.DotDoAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId, cancellationToken)
                ?? throw new NotFoundException("Đợt đồ án", periodId);
            if (date.Date < period.NgayBatDauBaoVe.Date || date.Date > period.NgayKetThucBaoVe.Date)
                throw new BusinessRuleException(
                    $"Ngày bảo vệ phải nằm trong khoảng {period.NgayBatDauBaoVe:dd/MM/yyyy} - {period.NgayKetThucBaoVe:dd/MM/yyyy}.");
        }

        private static CommitteeDto MapToCommitteeDto(HoiDong x)
        {
            var members = x.ThanhViens.Select(m => new CommitteeMemberDto(
                m.Id,
                m.MaGV,
                m.VaiTro switch
                {
                    CommitteeRole.CHU_TICH => "CHU_TICH",
                    CommitteeRole.THU_KY => "THU_KY",
                    _ => "UY_VIEN"
                })).ToList();

            return new CommitteeDto(
                x.Id,
                x.MaHoiDongCode,
                x.TenHoiDong,
                x.MaDotDoAn,
                x.DiaDiem,
                x.TrangThai.ToString().ToUpperInvariant(),
                members,
                x.IsReadyForDefense,
                HoiDong.GetCompositionErrors(x.ThanhViens.Select(m => m.VaiTro)));
        }

        private static DefenseScheduleDto MapToScheduleDto(LichBaoVe x)
        {
            return new DefenseScheduleDto(
                x.Id,
                x.MaDoAn,
                x.MaHoiDong,
                x.MaDotDoAn,
                x.NgayBaoVe,
                x.GioBatDau,
                x.GioKetThuc,
                x.Phong,
                x.TrangThai.ToString().ToUpperInvariant());
        }
    }
}
