using Microsoft.AspNetCore.Mvc;
using Upm.Application.Workflow;

namespace Upm.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class WorkflowController(IWorkflowService workflow) : ControllerBase
{
    [HttpGet("workflow/dinh-nghia")]
    [ProducesResponseType<IReadOnlyList<Upm.Domain.Workflow.WorkflowTransitionDefinition>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Upm.Domain.Workflow.WorkflowTransitionDefinition>> LayDinhNghia() =>
        Ok(workflow.LayDinhNghia());

    [HttpGet("svtrongdot/{id:long}/lich-su-trang-thai")]
    [ProducesResponseType<IReadOnlyList<WorkflowHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkflowHistoryItem>>> LayLichSu(long id, CancellationToken ct) =>
        Ok(await workflow.LayLichSuAsync(id, ct));

    [HttpGet("svtrongdot/{id:long}/chuyen-hop-le")]
    [ProducesResponseType<WorkflowAvailableTransitions>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkflowAvailableTransitions>> LayChuyenHopLe(long id, CancellationToken ct) =>
        Ok(await workflow.LayChuyenHopLeAsync(id, ct));

    [HttpPost("svtrongdot/{id:long}/chuyen-trang-thai")]
    [ProducesResponseType<WorkflowRecordView>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkflowRecordView>> Transition(
        long id,
        [FromBody] WorkflowTransitionRequest request,
        CancellationToken ct) =>
        Ok(await workflow.TransitionAsync(id, request, ct));

    [HttpPost("svtrongdot/{id:long}/lam-lai")]
    [ProducesResponseType<WorkflowRecordView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkflowRecordView>> TaoHoSoLamLai(
        long id,
        [FromBody] TaoHoSoLamLaiRequest request,
        CancellationToken ct)
    {
        var result = await workflow.TaoHoSoLamLaiAsync(id, request.DotMoiId, ct);
        return CreatedAtAction(nameof(LayLichSu), new { id = result.Id }, result);
    }
}

public sealed record TaoHoSoLamLaiRequest(long DotMoiId);
