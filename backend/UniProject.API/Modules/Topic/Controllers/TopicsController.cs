using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Topic
{
    [ApiController]
    [Route("api/v1/topics")]
    [Tags("Topics")]
    public class TopicsController : ControllerBase
    {
        private readonly ITopicService _service;

        public TopicsController(ITopicService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<TopicDto>), 200)]
        public async Task<IActionResult> Get(
            [FromQuery] PaginationParams @params,
            [FromQuery] int? lecturerId,
            [FromQuery] int? majorId,
            [FromQuery] string? status,
            [FromQuery] string? projectKind,
            CancellationToken ct)
        {
            var result = await _service.GetTopicsAsync(@params, lecturerId, majorId, status, projectKind, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Result<TopicDto>), 200)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetTopicByIdAsync(id, ct);
            return Ok(Result<TopicDto>.Ok(result));
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost]
        [ProducesResponseType(typeof(Result<TopicDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken ct)
        {
            var result = await _service.CreateTopicAsync(request, ct);
            return StatusCode(201, Result<TopicDto>.Ok(result, "Tạo đề tài thành công. Trạng thái: CHO_DUYET."));
        }

        /// <summary>Sinh viên đề xuất đề tài, chỉ định giảng viên dự kiến hướng dẫn.</summary>
        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("student-proposals")]
        [ProducesResponseType(typeof(Result<TopicDto>), 201)]
        public async Task<IActionResult> ProposeByStudent([FromBody] StudentProposeTopicRequest request, CancellationToken ct)
        {
            var result = await _service.ProposeTopicByStudentAsync(request, ct);
            return StatusCode(201, Result<TopicDto>.Ok(result, "Đề xuất đề tài thành công. Trạng thái: CHO_DUYET."));
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(Result<TopicDto>), 200)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTopicRequest request, CancellationToken ct)
        {
            var result = await _service.UpdateTopicAsync(id, request, ct);
            return Ok(Result<TopicDto>.Ok(result, "Cập nhật đề tài thành công."));
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Approve(int id, CancellationToken ct)
        {
            var result = await _service.ApproveTopicAsync(id, ct);
            return Ok(result);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectTopicRequest request, CancellationToken ct)
        {
            var result = await _service.RejectTopicAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("{id:int}/disable")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Disable(int id, CancellationToken ct)
        {
            var result = await _service.DisableTopicAsync(id, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("periods")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AddToPeriod([FromBody] AddTopicToPeriodRequest request, CancellationToken ct)
        {
            var result = await _service.AddTopicToPeriodAsync(request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPatch("period-topics/{id:int}/status")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> ChangePeriodTopicStatus(int id, [FromBody] ChangePeriodTopicStatusRequest request, CancellationToken ct)
        {
            var result = await _service.ChangePeriodTopicStatusAsync(id, request, ct);
            return Ok(result);
        }

        [HttpGet("periods/{periodId:int}")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<PeriodTopicDto>>), 200)]
        public async Task<IActionResult> GetTopicsInPeriod(int periodId, [FromQuery] string? status, CancellationToken ct)
        {
            var result = await _service.GetTopicsInPeriodAsync(periodId, status, ct);
            return Ok(Result<IReadOnlyList<PeriodTopicDto>>.Ok(result));
        }
    }
}
