using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Grading
{
    [ApiController]
    [Route("api/v1/projects/{projectId:int}")]
    [Tags("Grading")]
    public class GradingController : ControllerBase
    {
        private readonly IGradingService _service;

        public GradingController(IGradingService service)
        {
            _service = service;
        }

        [Authorize(Roles = AppRoles.GiangVien)]
        [HttpPost("grades")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<EvaluationScoreDto>>), 200)]
        public async Task<IActionResult> SubmitGrade(int projectId, [FromBody] SubmitGradeRequest request, CancellationToken ct)
        {
            // Trả về các phiếu đã ghi: một phiếu khi chấm riêng, mỗi thành viên một phiếu khi chấm chung.
            var result = await _service.SubmitGradeAsync(projectId, request, ct);
            return Ok(Result<IReadOnlyList<EvaluationScoreDto>>.Ok(result, $"Nhập điểm thành công cho {result.Count} sinh viên."));
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpGet("grades")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<EvaluationScoreDto>>), 200)]
        public async Task<IActionResult> GetGrades(int projectId, CancellationToken ct)
        {
            var result = await _service.GetProjectGradesAsync(projectId, ct);
            return Ok(Result<IReadOnlyList<EvaluationScoreDto>>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("publish-result")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> PublishResult(int projectId, CancellationToken ct)
        {
            var result = await _service.PublishResultAsync(projectId, ct);
            return Ok(result);
        }

        [HttpGet("results")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<FinalResultDto>>), 200)]
        public async Task<IActionResult> GetResults(int projectId, CancellationToken ct)
        {
            var result = await _service.GetProjectResultsAsync(projectId, ct);
            return Ok(Result<IReadOnlyList<FinalResultDto>>.Ok(result));
        }
    }
}
