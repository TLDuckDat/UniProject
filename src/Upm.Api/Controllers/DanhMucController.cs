using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.Common;
using Upm.Application.DanhMuc;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

[ApiController]
[Route("api/v1/danh-muc")]
[CoQuyen(Quyen.DanhMucQuanLy)]
[Tags("DanhMuc")]
public sealed class DanhMucController(IDanhMucService service) : ControllerBase
{
    [HttpGet("{loai}")]
    public Task<PagedResult<DanhMucItem>> DanhSach(string loai, [FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? sort = null, [FromQuery] bool desc = false, CancellationToken ct = default) =>
        service.DanhSachAsync(loai, new DanhMucQuery(q, page, pageSize, sort, desc), ct);

    [HttpGet("{loai}/{id:long}")]
    public Task<DanhMucItem> Lay(string loai, long id, CancellationToken ct) => service.LayAsync(loai, id, ct);

    [HttpPost("{loai}")]
    public async Task<ActionResult<DanhMucItem>> Tao(string loai, DanhMucRequest request, CancellationToken ct)
    {
        var item = await service.TaoAsync(loai, request, ct);
        return CreatedAtAction(nameof(Lay), new { loai, id = item.Id }, item);
    }

    [HttpPut("{loai}/{id:long}")]
    public Task<DanhMucItem> Sua(string loai, long id, DanhMucRequest request, CancellationToken ct) => service.SuaAsync(loai, id, request, ct);

    [HttpDelete("{loai}/{id:long}")]
    public async Task<IActionResult> Xoa(string loai, long id, CancellationToken ct)
    {
        await service.XoaAsync(loai, id, ct);
        return NoContent();
    }
}
