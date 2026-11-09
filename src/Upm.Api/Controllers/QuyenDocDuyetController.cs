using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.QuyenDocDuyet;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

/// <summary>Form nộp quyển (multipart/form-data): một file PDF hoặc DOCX.</summary>
public sealed class NopQuyenForm
{
    public IFormFile? File { get; set; }
}

/// <summary>UC-3b · Nộp quyển, ý kiến GVHD, đọc duyệt GVĐD, đạo văn, bảo vệ, GVĐD trễ hạn.</summary>
[ApiController]
[Route("api/v1/svtrongdot")]
[Tags("QuyenDocDuyet")]
public sealed class QuyenHoSoController(IQuyenService service) : ControllerBase
{
    /// <summary>SV nộp quyển (đang thực hiện) hoặc nộp bản sửa gấp (đang yêu cầu chỉnh sửa).</summary>
    [HttpPost("{id:long}/quyen", Name = "quyenNop")]
    [CoQuyen(Quyen.HoSoCuaToi)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PhienBanQuyenItem>> NopQuyen(long id, [FromForm] NopQuyenForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return Problem(title: "Thiếu file quyển.", statusCode: StatusCodes.Status400BadRequest);
        await using var stream = form.File.OpenReadStream();
        var pb = await service.NopQuyenAsync(id, stream, form.File.FileName, form.File.ContentType ?? "application/octet-stream", ct);
        return Ok(pb);
    }

    [HttpGet("{id:long}/quyen/lich-su", Name = "quyenLichSu")]
    public Task<IReadOnlyList<PhienBanQuyenItem>> LichSu(long id, CancellationToken ct) => service.LichSuPhienBanAsync(id, ct);

    /// <summary>GVHD (còn hiệu lực) ghi ý kiến; không đồng ý phải có lý do (D05).</summary>
    [HttpPost("{id:long}/y-kien-gvhd", Name = "quyenYKienGvhd")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<YKienGvhdItem> YKienGvhd(long id, YKienGvhdRequest request, CancellationToken ct) =>
        service.YKienGvhdAsync(id, request, ct);

    /// <summary>GVĐD (còn hiệu lực) đọc duyệt: đồng ý (cần điểm), sửa gấp (vòng 1), hoặc không đồng ý.</summary>
    [HttpPost("{id:long}/doc-duyet", Name = "quyenDocDuyet")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<DocDuyetItem> DocDuyet(long id, DocDuyetRequest request, CancellationToken ct) =>
        service.DocDuyetAsync(id, request, ct);

    [HttpPost("{id:long}/dao-van/nhap-tay", Name = "quyenNhapTayDaoVan")]
    public Task<KiemTraDaoVanItem> NhapTayDaoVan(long id, NhapTayDaoVanRequest request, CancellationToken ct) =>
        service.NhapTayDaoVanAsync(id, request, ct);

    /// <summary>Kiểm tra điều kiện bảo vệ liên quan quyển: đạo văn (D11), đánh dấu bản bảo vệ và hash (D13).</summary>
    [HttpGet("{id:long}/kiem-tra-bao-ve", Name = "quyenKiemTraBaoVe")]
    public Task<KiemTraBaoVeItem> KiemTraBaoVe(long id, CancellationToken ct) => service.KiemTraBaoVeAsync(id, ct);

    [HttpPost("{id:long}/ban-bao-ve", Name = "quyenDanhDauBanBaoVe")]
    public Task<BanBaoVeItem> DanhDauBanBaoVe(long id, CancellationToken ct) => service.DanhDauBanBaoVeAsync(id, ct);

    /// <summary>Trưởng BM thay GVĐD quá hạn đọc (D09). SV không bị tính lỗi.</summary>
    [HttpPost("{id:long}/thay-gvdd", Name = "quyenThayGvdd")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ThayGvdd(long id, ThayGvddRequest request, CancellationToken ct)
    {
        await service.ThayGvddAsync(id, request, ct);
        return NoContent();
    }
}

/// <summary>Hàng đợi quyển của GVĐD, tải phiên bản quyển, danh sách GVĐD trễ hạn.</summary>
[ApiController]
[Route("api/v1/quyen")]
[Tags("QuyenDocDuyet")]
public sealed class QuyenDocDuyetController(IQuyenService service) : ControllerBase
{
    [HttpGet("can-doc", Name = "quyenCanDoc")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<QuyenCanDocItem>> CanDoc([FromQuery] long dotId, CancellationToken ct) =>
        service.QuyenCanDocAsync(dotId, ct);

    /// <summary>Tải một phiên bản quyển để đọc trên trình duyệt (xem PDF).</summary>
    [HttpGet("{id:long}/tai", Name = "quyenTaiPhienBan")]
    public async Task<IActionResult> TaiPhienBan(long id, CancellationToken ct)
    {
        var (noi, contentType, tenFile) = await service.TaiPhienBanAsync(id, ct);
        return File(noi, contentType, tenFile, enableRangeProcessing: true);
    }

    [HttpGet("gvdd-tre-han", Name = "quyenGvddTreHan")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<GvddTreHanItem>> GvddTreHan([FromQuery] long dotId, CancellationToken ct) =>
        service.GvddTreHanAsync(dotId, ct);
}
