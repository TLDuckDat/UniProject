using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Defense
{
    [ApiController]
    [Route("api/v1/defenses")]
    [Tags("Defense")]
    public class DefenseController : ControllerBase
    {
        private readonly IDefenseService _service;

        public DefenseController(IDefenseService service)
        {
            _service = service;
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("projects/{projectId:int}/reviewers")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AssignReviewer(int projectId, [FromBody] AssignReviewerRequest request, CancellationToken ct)
        {
            var result = await _service.AssignReviewerAsync(projectId, request, ct);
            return Ok(result);
        }

        [HttpGet("projects/{projectId:int}/reviewers")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ReviewerAssignmentDto>>), 200)]
        public async Task<IActionResult> GetReviewers(int projectId, CancellationToken ct)
        {
            var result = await _service.GetReviewersAsync(projectId, ct);
            return Ok(Result<IReadOnlyList<ReviewerAssignmentDto>>.Ok(result));
        }

        [HttpGet("committees")]
        [ProducesResponseType(typeof(PagedResult<CommitteeDto>), 200)]
        public async Task<IActionResult> GetCommittees([FromQuery] PaginationParams @params, [FromQuery] int? periodId, CancellationToken ct)
        {
            var result = await _service.GetCommitteesAsync(@params, periodId, ct);
            return Ok(result);
        }

        [HttpGet("committees/{id:int}")]
        [ProducesResponseType(typeof(Result<CommitteeDto>), 200)]
        public async Task<IActionResult> GetCommitteeById(int id, CancellationToken ct)
        {
            var result = await _service.GetCommitteeByIdAsync(id, ct);
            return Ok(Result<CommitteeDto>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("committees")]
        [ProducesResponseType(typeof(Result<CommitteeDto>), 201)]
        public async Task<IActionResult> CreateCommittee([FromBody] CreateCommitteeRequest request, CancellationToken ct)
        {
            var result = await _service.CreateCommitteeAsync(request, ct);
            return StatusCode(201, Result<CommitteeDto>.Ok(result, "Tạo hội đồng bảo vệ thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("committees/{id:int}/members")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AddCommitteeMember(int id, [FromBody] AddCommitteeMemberRequest request, CancellationToken ct)
        {
            var result = await _service.AddCommitteeMemberAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("committees/{id:int}/members/{memberId:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> RemoveCommitteeMember(int id, int memberId, CancellationToken ct)
        {
            var result = await _service.RemoveCommitteeMemberAsync(id, memberId, ct);
            return Ok(result);
        }

        [HttpGet("schedules")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<DefenseScheduleDto>>), 200)]
        public async Task<IActionResult> GetSchedules(
            [FromQuery] int? periodId,
            [FromQuery] int? committeeId,
            [FromQuery] int? projectId,
            [FromQuery] DateTime? date,
            CancellationToken ct)
        {
            var result = await _service.GetDefenseSchedulesAsync(periodId, committeeId, projectId, date, ct);
            return Ok(Result<IReadOnlyList<DefenseScheduleDto>>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("schedules")]
        [ProducesResponseType(typeof(Result<DefenseScheduleDto>), 201)]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateDefenseScheduleRequest request, CancellationToken ct)
        {
            var result = await _service.CreateDefenseScheduleAsync(request, ct);
            return StatusCode(201, Result<DefenseScheduleDto>.Ok(result, "Tạo lịch bảo vệ thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("schedules/{id:int}")]
        [ProducesResponseType(typeof(Result<DefenseScheduleDto>), 200)]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateDefenseScheduleRequest request, CancellationToken ct)
        {
            var result = await _service.UpdateDefenseScheduleAsync(id, request, ct);
            return Ok(Result<DefenseScheduleDto>.Ok(result, "Cập nhật lịch bảo vệ thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("schedules/{id:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> DeleteSchedule(int id, CancellationToken ct)
        {
            var result = await _service.DeleteDefenseScheduleAsync(id, ct);
            return Ok(result);
        }
    }
}
