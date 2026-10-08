using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.ProjectPeriod
{
    [ApiController]
    [Route("api/v1/project-periods")]
    [Tags("Project Periods")]
    public class ProjectPeriodsController : ControllerBase
    {
        private readonly IProjectPeriodService _service;

        public ProjectPeriodsController(IProjectPeriodService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProjectPeriodDto>), 200)]
        public async Task<IActionResult> Get(
            [FromQuery] PaginationParams @params,
            [FromQuery] int? semesterId,
            [FromQuery] string? status,
            [FromQuery] string? projectKind,
            CancellationToken ct)
        {
            var result = await _service.GetProjectPeriodsAsync(@params, semesterId, status, projectKind, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Result<ProjectPeriodDto>), 200)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetProjectPeriodByIdAsync(id, ct);
            return Ok(Result<ProjectPeriodDto>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(Result<ProjectPeriodDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateProjectPeriodRequest request, CancellationToken ct)
        {
            var result = await _service.CreateProjectPeriodAsync(request, ct);
            return StatusCode(201, Result<ProjectPeriodDto>.Ok(result, "Tạo đợt đồ án thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(Result<ProjectPeriodDto>), 200)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectPeriodRequest request, CancellationToken ct)
        {
            var result = await _service.UpdateProjectPeriodAsync(id, request, ct);
            return Ok(Result<ProjectPeriodDto>.Ok(result, "Cập nhật đợt đồ án thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangePeriodStatusRequest request, CancellationToken ct)
        {
            var result = await _service.ChangeStatusAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("{id:int}/classes/{classId:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AddClass(int id, int classId, CancellationToken ct)
        {
            var result = await _service.AddClassToPeriodAsync(id, classId, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}/classes/{classId:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> RemoveClass(int id, int classId, CancellationToken ct)
        {
            var result = await _service.RemoveClassFromPeriodAsync(id, classId, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}/classes")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<int>>), 200)]
        public async Task<IActionResult> GetClasses(int id, CancellationToken ct)
        {
            var result = await _service.GetClassesInPeriodAsync(id, ct);
            return Ok(Result<IReadOnlyList<int>>.Ok(result));
        }
    }

    [ApiController]
    [Route("api/v1/project-types")]
    [Tags("Project Types")]
    public class ProjectTypesController : ControllerBase
    {
        private readonly IProjectPeriodService _service;

        public ProjectTypesController(IProjectPeriodService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ProjectTypeDto>>), 200)]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await _service.GetProjectTypesAsync(ct);
            return Ok(Result<IReadOnlyList<ProjectTypeDto>>.Ok(result));
        }
    }
}
