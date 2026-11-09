using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.DongBo;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

[ApiController]
[Route("api/v1/dong-bo")]
[CoQuyen(Quyen.DanhMucQuanLy)]
[Tags("DongBo")]
public sealed class DongBoController(IQldtGateway gateway) : ControllerBase
{
    [HttpGet("{loai}/mau")]
    public IActionResult Mau(LoaiImport loai) => File(gateway.TaoFileMau(loai), "text/csv; charset=utf-8", $"mau-{loai}.csv");

    [HttpPost("{loai}/xem-truoc")]
    [Consumes("multipart/form-data")]
    public async Task<ImportPreview> XemTruoc(LoaiImport loai, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) throw new BadHttpRequestException("File import đang trống.");
        await using var stream = file.OpenReadStream();
        return await gateway.XemTruocAsync(loai, file.FileName, stream, ct);
    }

    [HttpPost("xac-nhan")]
    public Task<ImportCommitResult> XacNhan([FromBody] ImportConfirmRequest request, CancellationToken ct) => gateway.XacNhanAsync(request.PreviewId, ct);
}

public sealed record ImportConfirmRequest(Guid PreviewId);
