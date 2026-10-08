using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.Share.Application.Interfaces;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;

namespace UniProject.API.Modules.Project
{
    public class ProjectService : IProjectService
    {
        private const long MaxUploadBytes = 20 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".rar", ".png", ".jpg", ".jpeg"
        };

        private readonly AppDbContext _dbContext;
        private readonly IFileStorage _fileStorage;
        private readonly CurrentActor _actor;
        private readonly ProjectAccess _access;
        private readonly ProjectTypePolicyResolver _policies;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            AppDbContext dbContext,
            IFileStorage fileStorage,
            CurrentActor actor,
            ProjectAccess access,
            ProjectTypePolicyResolver policies,
            ILogger<ProjectService> logger)
        {
            _dbContext = dbContext;
            _fileStorage = fileStorage;
            _actor = actor;
            _access = access;
            _policies = policies;
            _logger = logger;
        }

        /// <summary>Sinh viên luôn chỉ thấy đồ án của mình, bất kể tham số studentId gửi lên.</summary>
        public async Task<PagedResult<ProjectDto>> GetProjectsAsync(
            PaginationParams @params,
            int? periodId = null,
            int? lecturerId = null,
            int? studentId = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            if (!_actor.IsAdmin && !_actor.IsLecturer)
                studentId = await _actor.GetStudentIdAsync(cancellationToken);

            var query = _dbContext.DoAns
                .AsNoTracking()
                .Include(x => x.ThanhViens)
                .Include(x => x.PhanCongHuongDans)
                .AsQueryable();

            if (periodId.HasValue)
                query = query.Where(x => x.MaDotDoAn == periodId.Value);
            if (lecturerId.HasValue)
                query = query.Where(x => x.PhanCongHuongDans.Any(a => a.MaGV == lecturerId.Value && a.TrangThai));
            if (studentId.HasValue)
                query = query.Where(x => x.ThanhViens.Any(m => m.MaSV == studentId.Value));
            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = ParseEnum<ProjectStatus>(status, "Status");
                query = query.Where(x => x.TrangThai == parsed);
            }
            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaDoAnCode.ToLower().Contains(s) || x.TenDoAn.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.NgayTao)
                : query.OrderBy(x => x.NgayTao);

            return await query.ToPagedResultAsync(@params.PageNumber, @params.PageSize, x => MapToDto(x), cancellationToken);
        }

        public async Task<ProjectDto> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await _access.EnsureCanViewAsync(id, cancellationToken);
            return MapToDto(await LoadProjectAsync(id, cancellationToken));
        }

        /// <summary>
        /// Đề tài phải đang được mở trong đợt; số thành viên không vượt sức chứa đề tài và quy mô của loại đồ án;
        /// mọi thành viên phải thỏa điều kiện tiên quyết của loại đồ án.
        /// Quy tắc 2: mỗi SV chỉ thuộc một đồ án (chưa hủy) trong cùng một đợt.
        /// Nếu tạo từ đơn đăng ký thì đơn đó phải đã được duyệt và cùng đợt.
        /// </summary>
        public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.DoAns.AnyAsync(x => x.MaDoAnCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã đồ án '{request.Code}' đã tồn tại.");

            if (request.Members.Count == 0)
                throw new ValidationException("Members", "Đồ án phải có ít nhất một sinh viên.");
            var studentIds = request.Members.Select(m => m.StudentId).Distinct().ToList();
            if (studentIds.Count != request.Members.Count)
                throw new ValidationException("Members", "Danh sách thành viên bị trùng sinh viên.");

            var topic = await _dbContext.DeTaiDotDoAns.AsNoTracking()
                .Where(x => x.MaDeTai == request.TopicId && x.MaDotDoAn == request.PeriodId)
                .Select(x => x.DeTai)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new BusinessRuleException("Đề tài chưa được mở trong đợt đồ án này.");

            var existingStudents = await _dbContext.SinhViens.CountAsync(x => studentIds.Contains(x.Id), cancellationToken);
            if (existingStudents != studentIds.Count)
                throw new NotFoundException("Có sinh viên trong danh sách thành viên không tồn tại.");

            var takenStudent = await _dbContext.ThanhVienDoAns
                .Where(t => studentIds.Contains(t.MaSV) && t.DoAn.MaDotDoAn == request.PeriodId && t.DoAn.TrangThai != ProjectStatus.DA_HUY)
                .Select(t => (int?)t.MaSV)
                .FirstOrDefaultAsync(cancellationToken);
            if (takenStudent.HasValue)
                throw new BusinessRuleException($"Sinh viên ID {takenStudent} đã thuộc đồ án khác trong đợt này.");

            var policy = await _policies.ForPeriodAsync(_dbContext, request.PeriodId, cancellationToken);
            foreach (var studentId in studentIds)
                await policy.EnsureStudentEligibleAsync(studentId, cancellationToken);
            var maxMembers = Math.Min(topic.SoLuongSinhVienToiDa, policy.MaxMembers);

            if (request.RegistrationId.HasValue)
            {
                var registration = await _dbContext.DangKyDeTais.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.RegistrationId.Value, cancellationToken)
                    ?? throw new NotFoundException("Đăng ký đề tài", request.RegistrationId.Value);
                if (registration.TrangThai != RegistrationStatus.DA_DUYET || registration.MaDotDoAn != request.PeriodId)
                    throw new BusinessRuleException("Đơn đăng ký phải đã được duyệt và thuộc cùng đợt đồ án.");
            }

            if (request.PrimaryAdvisorLecturerId.HasValue &&
                !await _dbContext.GiangViens.AnyAsync(x => x.Id == request.PrimaryAdvisorLecturerId.Value, cancellationToken))
                throw new NotFoundException("Giảng viên", request.PrimaryAdvisorLecturerId.Value);

            var project = new DoAn(request.Code, request.Title, request.PeriodId, request.TopicId, request.RegistrationId);
            foreach (var m in request.Members)
            {
                var role = m.Role.ToUpperInvariant() == "TRUONG_NHOM" ? ProjectMemberRole.TRUONG_NHOM : ProjectMemberRole.THANH_VIEN;
                project.AddMember(m.StudentId, role, maxMembers);
            }
            if (request.PrimaryAdvisorLecturerId.HasValue)
                project.AssignAdvisor(request.PrimaryAdvisorLecturerId.Value, AdvisorType.CHINH);

            _dbContext.DoAns.Add(project);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created project {Code} - {Title}", project.MaDoAnCode, project.TenDoAn);
            return MapToDto(project);
        }

        public async Task<Result> AssignAdvisorAsync(int projectId, AssignAdvisorRequest request, CancellationToken cancellationToken = default)
        {
            var project = await _dbContext.DoAns
                .Include(x => x.PhanCongHuongDans)
                .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken)
                ?? throw new NotFoundException("Đồ án", projectId);

            if (!await _dbContext.GiangViens.AnyAsync(x => x.Id == request.LecturerId, cancellationToken))
                throw new NotFoundException("Giảng viên", request.LecturerId);

            var type = request.AdvisorType.ToUpperInvariant() == "CHINH" ? AdvisorType.CHINH : AdvisorType.PHU;
            project.AssignAdvisor(request.LecturerId, type);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned advisor {LecturerId} ({Type}) to project {ProjectId}", request.LecturerId, type, projectId);
            return Result.Ok("Phân công giảng viên hướng dẫn thành công.");
        }

        /// <summary>Xóa mềm: chỉ đánh dấu hết hiệu lực để giữ lịch sử phân công.</summary>
        public async Task<Result> RemoveAdvisorAsync(int projectId, int advisorAssignmentId, CancellationToken cancellationToken = default)
        {
            var assignment = await _dbContext.PhanCongHuongDans
                .FirstOrDefaultAsync(x => x.Id == advisorAssignmentId && x.MaDoAn == projectId, cancellationToken)
                ?? throw new NotFoundException("Phân công hướng dẫn", advisorAssignmentId);

            assignment.Deactivate();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Hủy phân công giảng viên hướng dẫn thành công.");
        }

        /// <summary>
        /// Chỉ thành viên đồ án được nộp. Báo cáo đầu tiên tự chuyển đồ án từ CHO_THUC_HIEN sang DANG_THUC_HIEN.
        /// </summary>
        public async Task<ProgressReportDto> CreateProgressReportAsync(int projectId, CreateProgressReportRequest request, CancellationToken cancellationToken = default)
        {
            var project = await LoadProjectAsync(projectId, cancellationToken, tracking: true);
            var studentId = await _access.EnsureMemberAsync(projectId, cancellationToken);
            EnsureProjectActive(project);

            var report = new BaoCaoTienDo(
                projectId,
                studentId,
                request.ReportNumber,
                request.Title,
                request.Content,
                request.WorkDone,
                request.NextTasks,
                request.Difficulties,
                request.CompletionPercentage);
            _dbContext.BaoCaoTienDos.Add(report);

            if (project.TrangThai == ProjectStatus.CHO_THUC_HIEN)
                project.ChangeStatus(ProjectStatus.DANG_THUC_HIEN);

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Progress report #{Num} created for project {ProjectId}", request.ReportNumber, projectId);

            return MapToReportDto(report);
        }

        public async Task<IReadOnlyList<ProgressReportDto>> GetProgressReportsAsync(int projectId, CancellationToken cancellationToken = default)
        {
            await _access.EnsureCanViewAsync(projectId, cancellationToken);

            var list = await _dbContext.BaoCaoTienDos
                .AsNoTracking()
                .Include(x => x.NhanXetTienDos)
                .Where(x => x.MaDoAn == projectId)
                .OrderBy(x => x.LanBaoCao)
                .ToListAsync(cancellationToken);

            return list.Select(MapToReportDto).ToList();
        }

        /// <summary>Quy tắc 9: chỉ GV hướng dẫn đang phụ trách đồ án mới được nhận xét.</summary>
        public async Task<Result> ReviewProgressReportAsync(int reportId, CreateProgressReviewRequest request, CancellationToken cancellationToken = default)
        {
            var report = await _dbContext.BaoCaoTienDos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == reportId, cancellationToken)
                ?? throw new NotFoundException("Báo cáo tiến độ", reportId);

            var lecturerId = await _access.EnsureActiveAdvisorAsync(report.MaDoAn, cancellationToken);
            var evaluation = ParseEnum<ProgressEvaluation>(request.Evaluation, "Evaluation");

            _dbContext.NhanXetTienDos.Add(new NhanXetTienDo(reportId, lecturerId, request.ReviewContent, evaluation));
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Progress review added for report {ReportId} by lecturer {LecturerId}", reportId, lecturerId);
            return Result.Ok("Gửi nhận xét báo cáo tiến độ thành công.");
        }

        public async Task<RevisionRequestDto> CreateRevisionRequestAsync(int projectId, CreateRevisionRequest request, CancellationToken cancellationToken = default)
        {
            await _access.EnsureExistsAsync(projectId, cancellationToken);
            var lecturerId = await _access.EnsureActiveAdvisorAsync(projectId, cancellationToken);

            var item = new YeuCauChinhSua(projectId, lecturerId, request.Title, request.Content, request.Deadline);
            _dbContext.YeuCauChinhSuas.Add(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RevisionRequestDto(item.Id, item.MaDoAn, item.MaGVYeuCau, item.TieuDe, item.NoiDung, item.HanHoanThanh, item.NgayYeuCau, item.TrangThai.ToString());
        }

        /// <summary>
        /// Sinh viên trong đồ án báo tiến độ (DANG_THUC_HIEN, DA_HOAN_THANH);
        /// GV hướng dẫn xác nhận hoặc hủy (DA_XAC_NHAN, DA_HUY); Admin đổi được mọi trạng thái.
        /// </summary>
        public async Task<Result> UpdateRevisionStatusAsync(int revisionId, UpdateRevisionStatusRequest request, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.YeuCauChinhSuas.FindAsync(new object[] { revisionId }, cancellationToken)
                ?? throw new NotFoundException("Yêu cầu chỉnh sửa", revisionId);
            var status = ParseEnum<RevisionStatus>(request.Status, "Status");

            if (!_actor.IsAdmin)
            {
                if (status is RevisionStatus.DANG_THUC_HIEN or RevisionStatus.DA_HOAN_THANH)
                    await _access.EnsureMemberAsync(item.MaDoAn, cancellationToken);
                else if (status is RevisionStatus.DA_XAC_NHAN or RevisionStatus.DA_HUY)
                    await _access.EnsureActiveAdvisorAsync(item.MaDoAn, cancellationToken);
                else
                    throw new ForbiddenException("Bạn không được chuyển yêu cầu chỉnh sửa về trạng thái này.");
            }

            item.ChangeStatus(status);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Cập nhật trạng thái yêu cầu chỉnh sửa thành công.");
        }

        /// <summary>
        /// Chỉ thành viên đồ án được nộp; giới hạn 20MB và các định dạng tài liệu thông dụng.
        /// File lưu trên đĩa tại uploads/projects/{projectId}; CSDL chỉ lưu đường dẫn tương đối.
        /// </summary>
        public async Task<ProjectDocumentDto> UploadDocumentAsync(int projectId, UploadDocumentRequest request, IFormFile file, CancellationToken cancellationToken = default)
        {
            var project = await LoadProjectAsync(projectId, cancellationToken);
            var studentId = await _access.EnsureMemberAsync(projectId, cancellationToken);
            EnsureProjectActive(project);

            if (file.Length == 0)
                throw new ValidationException("File", "Tệp tải lên đang trống.");
            if (file.Length > MaxUploadBytes)
                throw new ValidationException("File", "Tệp tải lên vượt quá 20MB.");
            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
                throw new ValidationException("File", $"Chỉ chấp nhận các định dạng: {string.Join(", ", AllowedExtensions)}.");
            if (string.IsNullOrWhiteSpace(request.DocumentName))
                throw new ValidationException("DocumentName", "Tên tài liệu không được để trống.");
            if (request.Version < 1)
                throw new ValidationException("Version", "Phiên bản phải từ 1 trở lên.");

            var docType = ParseEnum<DocumentType>(request.DocumentType, "DocumentType");

            using var stream = file.OpenReadStream();
            var relativePath = await _fileStorage.SaveFileAsync(stream, file.FileName, $"projects/{projectId}", cancellationToken);

            var doc = new TaiLieuDoAn(projectId, studentId, request.DocumentName, relativePath, docType, request.Version);
            _dbContext.TaiLieuDoAns.Add(doc);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document '{Name}' uploaded for project {ProjectId}", request.DocumentName, projectId);
            return MapToDocumentDto(doc);
        }

        public async Task<IReadOnlyList<ProjectDocumentDto>> GetDocumentsAsync(int projectId, CancellationToken cancellationToken = default)
        {
            await _access.EnsureCanViewAsync(projectId, cancellationToken);

            var docs = await _dbContext.TaiLieuDoAns
                .AsNoTracking()
                .Where(x => x.MaDoAn == projectId)
                .OrderByDescending(x => x.NgayUpload)
                .ToListAsync(cancellationToken);
            return docs.Select(MapToDocumentDto).ToList();
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int projectId, int documentId, CancellationToken cancellationToken = default)
        {
            await _access.EnsureCanViewAsync(projectId, cancellationToken);

            var doc = await _dbContext.TaiLieuDoAns.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == documentId && x.MaDoAn == projectId, cancellationToken)
                ?? throw new NotFoundException("Tài liệu đồ án", documentId);

            return await _fileStorage.GetFileAsync(doc.DuongDanFile, cancellationToken);
        }

        private async Task<DoAn> LoadProjectAsync(int id, CancellationToken cancellationToken, bool tracking = false)
        {
            var query = _dbContext.DoAns.Include(x => x.ThanhViens).Include(x => x.PhanCongHuongDans).AsQueryable();
            if (!tracking) query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException("Đồ án", id);
        }

        private static void EnsureProjectActive(DoAn project)
        {
            if (project.TrangThai is ProjectStatus.DA_HUY or ProjectStatus.HOAN_THANH or ProjectStatus.TAM_DUNG)
                throw new BusinessRuleException($"Đồ án đang ở trạng thái {project.TrangThai}, không thể nộp thêm.");
        }

        private static TEnum ParseEnum<TEnum>(string value, string field) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, true, out var result) && Enum.IsDefined(result))
                return result;
            throw new ValidationException(field, $"Giá trị '{value}' không hợp lệ. Hợp lệ: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        private static ProgressReportDto MapToReportDto(BaoCaoTienDo r) => new(
            r.Id, r.MaDoAn, r.MaSVNop, r.LanBaoCao, r.TieuDe, r.NoiDung, r.CongViecDaLam, r.CongViecTiepTheo,
            r.KhoKhan, r.PhanTramHoanThanh, r.NgayNop,
            r.NhanXetTienDos.Select(n => new ProgressReviewDto(n.Id, n.MaBaoCao, n.MaGV, n.NoiDungNhanXet, n.MucDanhGia.ToString(), n.NgayNhanXet)).ToList());

        private static ProjectDocumentDto MapToDocumentDto(TaiLieuDoAn d) => new(
            d.Id, d.MaDoAn, d.MaSVNop, d.TenTaiLieu, d.DuongDanFile, d.LoaiTaiLieu.ToString(), d.Version, d.NgayUpload);

        private static ProjectDto MapToDto(DoAn x)
        {
            var members = x.ThanhViens.Select(m => new ProjectMemberDto(m.Id, m.MaSV, m.VaiTro.ToString())).ToList();
            var advisors = x.PhanCongHuongDans.Select(a => new AdvisorAssignmentDto(a.Id, a.MaGV, a.LoaiHuongDan.ToString(), a.NgayPhanCong, a.TrangThai)).ToList();
            return new ProjectDto(x.Id, x.MaDoAnCode, x.TenDoAn, x.MaDotDoAn, x.MaDeTai, x.MaDangKy, x.NgayTao,
                x.NgayBatDau, x.NgayKetThuc, x.TrangThai.ToString(), members, advisors);
        }
    }
}
