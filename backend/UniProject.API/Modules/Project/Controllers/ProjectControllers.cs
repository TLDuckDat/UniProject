using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Project
{
    [ApiController]
    [Route("api/v1/projects")]
    [Tags("Projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectsController(IProjectService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProjectDto>), 200)]
        public async Task<IActionResult> Get(
            [FromQuery] PaginationParams @params,
            [FromQuery] int? periodId,
            [FromQuery] int? lecturerId,
            [FromQuery] int? studentId,
            [FromQuery] string? status,
            CancellationToken ct)
        {
            var result = await _service.GetProjectsAsync(@params, periodId, lecturerId, studentId, status, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Result<ProjectDto>), 200)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetProjectByIdAsync(id, ct);
            return Ok(Result<ProjectDto>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(Result<ProjectDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
        {
            var result = await _service.CreateProjectAsync(request, ct);
            return StatusCode(201, Result<ProjectDto>.Ok(result, "Tạo đồ án thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("{id:int}/advisors")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AssignAdvisor(int id, [FromBody] AssignAdvisorRequest request, CancellationToken ct)
        {
            var result = await _service.AssignAdvisorAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}/advisors/{assignmentId:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> RemoveAdvisor(int id, int assignmentId, CancellationToken ct)
        {
            var result = await _service.RemoveAdvisorAsync(id, assignmentId, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("{id:int}/progress-reports")]
        [ProducesResponseType(typeof(Result<ProgressReportDto>), 201)]
        public async Task<IActionResult> CreateProgressReport(int id, [FromBody] CreateProgressReportRequest request, CancellationToken ct)
        {
            var result = await _service.CreateProgressReportAsync(id, request, ct);
            return StatusCode(201, Result<ProgressReportDto>.Ok(result, "Nộp báo cáo tiến độ thành công."));
        }

        [HttpGet("{id:int}/progress-reports")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ProgressReportDto>>), 200)]
        public async Task<IActionResult> GetProgressReports(int id, CancellationToken ct)
        {
            var result = await _service.GetProgressReportsAsync(id, ct);
            return Ok(Result<IReadOnlyList<ProgressReportDto>>.Ok(result));
        }

        [Authorize(Roles = AppRoles.GiangVien)]
        [HttpPost("{id:int}/revision-requests")]
        [ProducesResponseType(typeof(Result<RevisionRequestDto>), 201)]
        public async Task<IActionResult> CreateRevisionRequest(int id, [FromBody] CreateRevisionRequest request, CancellationToken ct)
        {
            var result = await _service.CreateRevisionRequestAsync(id, request, ct);
            return StatusCode(201, Result<RevisionRequestDto>.Ok(result, "Tạo yêu cầu chỉnh sửa thành công."));
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("{id:int}/documents")]
        [ProducesResponseType(typeof(Result<ProjectDocumentDto>), 201)]
        public async Task<IActionResult> UploadDocument(
            int id,
            [FromForm] string documentName,
            [FromForm] string documentType,
            [FromForm] int version,
            IFormFile file,
            CancellationToken ct)
        {
            var result = await _service.UploadDocumentAsync(id, new UploadDocumentRequest(documentName, documentType, version), file, ct);
            return StatusCode(201, Result<ProjectDocumentDto>.Ok(result, "Upload tài liệu thành công."));
        }

        [HttpGet("{id:int}/documents")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ProjectDocumentDto>>), 200)]
        public async Task<IActionResult> GetDocuments(int id, CancellationToken ct)
        {
            var result = await _service.GetDocumentsAsync(id, ct);
            return Ok(Result<IReadOnlyList<ProjectDocumentDto>>.Ok(result));
        }

        [HttpGet("{id:int}/documents/{documentId:int}/file")]
        public async Task<IActionResult> DownloadDocument(int id, int documentId, CancellationToken ct)
        {
            var (stream, contentType, fileName) = await _service.DownloadDocumentAsync(id, documentId, ct);
            return File(stream, contentType, fileName);
        }
    }

    [ApiController]
    [Route("api/v1/progress-reports")]
    [Tags("Projects - Progress Reports")]
    public class ProgressReportsController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProgressReportsController(IProjectService service)
        {
            _service = service;
        }

        [Authorize(Roles = AppRoles.GiangVien)]
        [HttpPost("{id:int}/reviews")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Review(int id, [FromBody] CreateProgressReviewRequest request, CancellationToken ct)
        {
            var result = await _service.ReviewProgressReportAsync(id, request, ct);
            return Ok(result);
        }
    }

    [ApiController]
    [Route("api/v1/revision-requests")]
    [Tags("Projects - Revision Requests")]
    public class RevisionRequestsController : ControllerBase
    {
        private readonly IProjectService _service;

        public RevisionRequestsController(IProjectService service)
        {
            _service = service;
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien},{AppRoles.SinhVien}")]
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRevisionStatusRequest request, CancellationToken ct)
        {
            var result = await _service.UpdateRevisionStatusAsync(id, request, ct);
            return Ok(result);
        }
    }
}
