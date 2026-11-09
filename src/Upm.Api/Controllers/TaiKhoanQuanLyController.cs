using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.Common;
using Upm.Application.DanhMuc;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

[ApiController]
[Route("api/v1/tai-khoan/quan-ly")]
[CoQuyen(Quyen.TaiKhoanQuanLy)]
[Tags("TaiKhoanQuanLy")]
public sealed class TaiKhoanQuanLyController(ITaiKhoanQuanLyService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<TaiKhoanItem>> DanhSach([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) => service.DanhSachAsync(q, new PageRequest(page, pageSize), ct);

    [HttpPost]
    public async Task<ActionResult<TaiKhoanItem>> Tao(TaiKhoanRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(DanhSach), await service.TaoAsync(request, ct));

    [HttpPut("{id:long}")]
    public Task<TaiKhoanItem> Sua(long id, TaiKhoanRequest request, CancellationToken ct) => service.SuaAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Xoa(long id, CancellationToken ct)
    {
        await service.XoaAsync(id, ct);
        return NoContent();
    }
}
