using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.Common;
using Upm.Application.RuleSets;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

[ApiController]
[Route("api/v1/rule-set")]
[CoQuyen(Quyen.RuleSetQuanLy)]
[Tags("RuleSet")]
public sealed class RuleSetController(IRuleSetService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<RuleSetItem>> DanhSach([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) => service.DanhSachAsync(q, new PageRequest(page, pageSize), ct);

    [HttpGet("{id:long}")]
    public Task<RuleSetItem> Lay(long id, CancellationToken ct) => service.LayAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<RuleSetItem>> Tao(RuleSetRequest request, CancellationToken ct)
    {
        var item = await service.TaoAsync(request, ct);
        return CreatedAtAction(nameof(Lay), new { id = item.Id }, item);
    }

    [HttpPut("{id:long}")]
    public Task<RuleSetItem> Sua(long id, RuleSetRequest request, CancellationToken ct) => service.SuaAsync(id, request, ct);

    [HttpPost("{id:long}/nhan-ban")]
    public Task<RuleSetItem> NhanBan(long id, RuleSetCloneRequest request, CancellationToken ct) => service.NhanBanAsync(id, request.MaRuleSet, request.TenRuleSet, ct);

    [HttpGet("{id:long}/khoa-hoc")]
    public Task<IReadOnlyList<RuleSetKhoaHocRequest>> KhoaHoc(long id, CancellationToken ct) => service.LayKhoaHocAsync(id, ct);

    [HttpPut("{id:long}/khoa-hoc")]
    public async Task<IActionResult> GanKhoaHoc(long id, IReadOnlyList<RuleSetKhoaHocRequest> items, CancellationToken ct)
    {
        await service.GanKhoaHocAsync(id, items, ct);
        return NoContent();
    }

    [HttpGet("{id:long}/tham-so-bo-mon")]
    public Task<IReadOnlyList<ThamSoBoMonItem>> ThamSoBoMon(long id, CancellationToken ct) => service.LayThamSoBoMonAsync(id, ct);

    [HttpPut("tham-so-bo-mon")]
    public Task<ThamSoBoMonItem> LuuThamSoBoMon(ThamSoBoMonRequest request, CancellationToken ct) => service.LuuThamSoBoMonAsync(request, ct);

    [HttpGet("tham-so-dao-van")]
    public Task<IReadOnlyList<ThamSoDaoVanItem>> ThamSoDaoVan([FromQuery] long khoaId, CancellationToken ct) => service.LayThamSoDaoVanAsync(khoaId, ct);

    [HttpPut("tham-so-dao-van")]
    public Task<ThamSoDaoVanItem> LuuThamSoDaoVan(ThamSoDaoVanRequest request, CancellationToken ct) => service.LuuThamSoDaoVanAsync(request, ct);
}

public sealed record RuleSetCloneRequest(string MaRuleSet, string TenRuleSet);
