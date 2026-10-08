using Microsoft.EntityFrameworkCore;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.Grading
{
    public class GradingService : IGradingService
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectGradeCalculator _calculator;
        private readonly CurrentActor _actor;
        private readonly ProjectAccess _access;
        private readonly ProjectTypePolicyResolver _policies;
        private readonly ILogger<GradingService> _logger;

        public GradingService(
            AppDbContext dbContext,
            IProjectGradeCalculator calculator,
            CurrentActor actor,
            ProjectAccess access,
            ProjectTypePolicyResolver policies,
            ILogger<GradingService> logger)
        {
            _dbContext = dbContext;
            _calculator = calculator;
            _actor = actor;
            _access = access;
            _policies = policies;
            _logger = logger;
        }

        /// <summary>
        /// Giảng viên đang đăng nhập nộp hoặc sửa phiếu chấm, đúng với vai trò của mình trong đồ án:
        /// HUONG_DAN → đang hướng dẫn; PHAN_BIEN → được phân công phản biện (chỉ loại đồ án có phản biện);
        /// HOI_DONG → thành viên hội đồng có lịch bảo vệ đồ án. Kết quả đã công bố thì không được sửa điểm.
        /// Chế độ chấm lấy từ policy loại đồ án: CHAM_RIENG chấm cho StudentId; CHAM_CHUNG áp dụng cùng điểm
        /// cho mọi thành viên đồ án (StudentId bỏ trống được, nếu gửi thì phải là thành viên).
        /// Sau đó tính lại KetQuaDoAn theo trọng số và ngưỡng đạt của loại đồ án; mọi phiếu chấm và kết quả
        /// lưu cùng một lần SaveChanges.
        /// </summary>
        public async Task<IReadOnlyList<EvaluationScoreDto>> SubmitGradeAsync(int projectId, SubmitGradeRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Score < 0 || request.Score > 10)
                throw new ValidationException("Score", "Điểm số phải từ 0 đến 10.");
            if (!Enum.TryParse<GradingType>(request.GradingType, true, out var gradingType) || !Enum.IsDefined(gradingType))
                throw new ValidationException("GradingType", $"Loại chấm điểm '{request.GradingType}' không hợp lệ.");

            await _access.EnsureExistsAsync(projectId, cancellationToken);
            var policy = await _policies.ForProjectAsync(_dbContext, projectId, cancellationToken);
            if (gradingType == GradingType.PHAN_BIEN && !policy.RequiresReviewer)
                throw new BusinessRuleException($"{policy.DisplayName} không có điểm phản biện.");

            var memberIds = await _dbContext.ThanhVienDoAns.Where(x => x.MaDoAn == projectId).Select(x => x.MaSV).ToListAsync(cancellationToken);
            if (request.StudentId.HasValue && !memberIds.Contains(request.StudentId.Value))
                throw new BusinessRuleException($"Sinh viên ID {request.StudentId} không thuộc đồ án này.");

            var targetIds = policy.GradingMode == GradingMode.CHAM_CHUNG
                ? memberIds
                : new List<int>
                {
                    request.StudentId ?? throw new ValidationException("StudentId", $"{policy.DisplayName} chấm riêng từng sinh viên, phải chọn sinh viên.")
                };

            var lecturerId = await _actor.GetLecturerIdAsync(cancellationToken);
            var committeeId = await EnsureCanGradeAsync(projectId, lecturerId, gradingType, cancellationToken);

            var results = await _dbContext.KetQuaDoAns
                .Where(x => x.MaDoAn == projectId && targetIds.Contains(x.MaSV))
                .ToListAsync(cancellationToken);
            if (results.Any(r => r.NgayCongBo != null))
                throw new BusinessRuleException("Kết quả đã được công bố, không thể sửa điểm.");

            var allScores = await _dbContext.PhieuChams
                .Where(x => x.MaDoAn == projectId && targetIds.Contains(x.MaSV))
                .ToListAsync(cancellationToken);

            var saved = new List<PhieuCham>();
            foreach (var studentId in targetIds)
            {
                var phieu = allScores.FirstOrDefault(x => x.MaSV == studentId && x.MaGV == lecturerId && x.LoaiCham == gradingType);
                if (phieu != null)
                {
                    phieu.UpdateScore(request.Score, request.Comments);
                }
                else
                {
                    phieu = new PhieuCham(projectId, studentId, lecturerId, committeeId, gradingType, request.Score, request.Comments);
                    _dbContext.PhieuChams.Add(phieu);
                    // Phiếu vừa thêm chưa SaveChanges nên truy vấn DB ở trên chưa trả về nó.
                    allScores.Add(phieu);
                }
                saved.Add(phieu);

                var scores = allScores.Where(x => x.MaSV == studentId).ToList();
                decimal? Average(GradingType type) =>
                    scores.Where(x => x.LoaiCham == type).Select(x => (decimal?)x.Diem).DefaultIfEmpty(null).Average();
                var avgAdvisor = Average(GradingType.HUONG_DAN);
                var avgReviewer = Average(GradingType.PHAN_BIEN);
                var avgCommittee = Average(GradingType.HOI_DONG);

                var ketQua = results.FirstOrDefault(x => x.MaSV == studentId);
                if (ketQua == null)
                {
                    ketQua = new KetQuaDoAn(projectId, studentId);
                    _dbContext.KetQuaDoAns.Add(ketQua);
                    results.Add(ketQua);
                }

                // Chỉ tính tổng kết khi đủ các cột điểm bắt buộc của loại đồ án, tránh ra KHONG_DAT giả khi còn thiếu điểm.
                var outcome = _calculator.Calculate(policy.Weights, policy.PassRules, avgAdvisor, avgReviewer, avgCommittee);
                if (outcome != null)
                    ketQua.UpdateGrades(avgAdvisor, avgReviewer, avgCommittee, outcome.FinalScore, outcome.Rank, outcome.Result);
                else
                    ketQua.MarkPending(avgAdvisor, avgReviewer, avgCommittee);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Grade {Score} submitted for students {StudentIds} project {ProjectId} by lecturer {LecturerId} ({Type}, {Mode})",
                request.Score, string.Join(",", targetIds), projectId, lecturerId, gradingType, policy.GradingMode);

            return saved.Select(MapToScoreDto).ToList();
        }

        public async Task<IReadOnlyList<EvaluationScoreDto>> GetProjectGradesAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var list = await _dbContext.PhieuChams
                .AsNoTracking()
                .Where(x => x.MaDoAn == projectId)
                .OrderBy(x => x.MaSV).ThenBy(x => x.LoaiCham)
                .ToListAsync(cancellationToken);
            return list.Select(MapToScoreDto).ToList();
        }

        /// <summary>Chỉ công bố khi mọi thành viên đồ án đã có đủ điểm và kết quả.</summary>
        public async Task<Result> PublishResultAsync(int projectId, CancellationToken cancellationToken = default)
        {
            await _access.EnsureExistsAsync(projectId, cancellationToken);

            var memberIds = await _dbContext.ThanhVienDoAns.Where(x => x.MaDoAn == projectId).Select(x => x.MaSV).ToListAsync(cancellationToken);
            var results = await _dbContext.KetQuaDoAns.Where(x => x.MaDoAn == projectId).ToListAsync(cancellationToken);

            var missing = memberIds
                .Where(id => results.All(r => r.MaSV != id || r.KetQua == FinalResult.CHO_KET_QUA))
                .ToList();
            if (missing.Count > 0)
                throw new BusinessRuleException($"Chưa đủ điểm để công bố. Sinh viên còn thiếu điểm: {string.Join(", ", missing)}.");

            foreach (var r in results)
                r.Publish();

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Published final results for project ID {ProjectId}", projectId);
            return Result.Ok("Công bố kết quả đồ án thành công.");
        }

        /// <summary>Admin/GV xem mọi kết quả; sinh viên chỉ thấy kết quả của chính mình sau khi đã công bố.</summary>
        public async Task<IReadOnlyList<FinalResultDto>> GetProjectResultsAsync(int projectId, CancellationToken cancellationToken = default)
        {
            await _access.EnsureCanViewAsync(projectId, cancellationToken);

            var query = _dbContext.KetQuaDoAns.AsNoTracking().Where(x => x.MaDoAn == projectId);
            if (!_actor.IsAdmin && !_actor.IsLecturer)
            {
                var studentId = await _actor.GetStudentIdAsync(cancellationToken);
                query = query.Where(x => x.MaSV == studentId && x.NgayCongBo != null);
            }

            var list = await query.ToListAsync(cancellationToken);
            return list.Select(x => new FinalResultDto(
                x.Id,
                x.MaDoAn,
                x.MaSV,
                x.DiemHuongDan,
                x.DiemPhanBien,
                x.DiemHoiDong,
                x.DiemTongKet,
                x.KetQua == FinalResult.CHO_KET_QUA ? "CHUA_XEP_LOAI" : x.XepLoai.ToString(),
                x.KetQua.ToString(),
                x.NgayCongBo)).ToList();
        }

        /// <summary>Kiểm tra giảng viên có đúng vai trò với loại chấm; trả về mã hội đồng nếu chấm hội đồng.</summary>
        private async Task<int?> EnsureCanGradeAsync(int projectId, int lecturerId, GradingType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case GradingType.HUONG_DAN:
                    if (!await _access.IsActiveAdvisorAsync(projectId, lecturerId, cancellationToken))
                        throw new ForbiddenException("Chỉ giảng viên hướng dẫn của đồ án mới được chấm điểm hướng dẫn.");
                    return null;

                case GradingType.PHAN_BIEN:
                    if (!await _dbContext.PhanBienDoAns.AnyAsync(x => x.MaDoAn == projectId && x.MaGV == lecturerId, cancellationToken))
                        throw new ForbiddenException("Chỉ giảng viên được phân công phản biện mới được chấm điểm phản biện.");
                    return null;

                default:
                    var committeeId = await _dbContext.LichBaoVes
                        .Where(x => x.MaDoAn == projectId && x.TrangThai != ScheduleStatus.DA_HUY)
                        .Select(x => (int?)x.MaHoiDong)
                        .FirstOrDefaultAsync(cancellationToken)
                        ?? throw new BusinessRuleException("Đồ án chưa có lịch bảo vệ nên chưa thể chấm điểm hội đồng.");
                    if (!await _dbContext.ThanhVienHoiDongs.AnyAsync(x => x.MaHoiDong == committeeId && x.MaGV == lecturerId, cancellationToken))
                        throw new ForbiddenException("Chỉ thành viên hội đồng bảo vệ đồ án này mới được chấm điểm hội đồng.");
                    return committeeId;
            }
        }

        private static EvaluationScoreDto MapToScoreDto(PhieuCham x) => new(
            x.Id, x.MaDoAn, x.MaSV, x.MaGV, x.MaHoiDong, x.LoaiCham.ToString(), x.Diem, x.NhanXet, x.NgayCham);
    }
}
