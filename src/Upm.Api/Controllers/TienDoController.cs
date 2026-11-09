using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.TienDo;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

/// <summary>Form nộp báo cáo tiến độ (multipart/form-data).</summary>
public sealed class NopBaoCaoForm
{
    public long MocTienDoId { get; set; }
    public decimal TyLeHoanThanhPct { get; set; }
    public bool CamKetAI { get; set; }
    public IFormFile? File { get; set; }
}

/// <summary>UC-3a · Mốc tiến độ, báo cáo 3 mốc, nhận xét GVHD, đơn gia hạn / xin dừng, thông báo.</summary>
[ApiController]
[Route("api/v1/tien-do")]
[Tags("TienDo")]
public sealed class TienDoController(ITienDoService service) : ControllerBase
{
    [HttpGet("moc", Name = "tienDoMocDanhSach")]
    public Task<IReadOnlyList<MocTienDoItem>> DanhSachMoc([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachMocAsync(dotId, ct);

    /// <summary>Phòng ĐT hoặc Trưởng BM sửa hạn / yêu cầu mốc (kiểm quyền trong service).</summary>
    [HttpPut("moc/{id:long}", Name = "tienDoMocCapNhat")]
    public Task<MocTienDoItem> CapNhatMoc(long id, CapNhatMocRequest request, CancellationToken ct) =>
        service.CapNhatMocAsync(id, request, ct);

    [HttpGet("gv", Name = "tienDoGvDanhSach")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<HoSoTienDoItem>> DanhSachGv([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachGvAsync(dotId, ct);

    [HttpGet("bo-mon", Name = "tienDoBoMonDanhSach")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<HoSoTienDoItem>> DanhSachBoMon([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachBoMonAsync(dotId, ct);

    [HttpGet("sv-cham", Name = "tienDoSvCham")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<SvCham>> SvCham([FromQuery] long dotId, CancellationToken ct) => service.SvChamAsync(dotId, ct);

    [HttpGet("cua-toi", Name = "tienDoCuaToi")]
    [CoQuyen(Quyen.HoSoCuaToi)]
    public Task<IReadOnlyList<HoSoTienDoItem>> CuaToi([FromQuery] long dotId, CancellationToken ct) =>
        service.TienDoCuaToiAsync(dotId, ct);

    [HttpGet("don", Name = "tienDoDonDanhSach")]
    public Task<IReadOnlyList<DonTienDoItem>> DanhSachDon([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachDonAsync(dotId, ct);

    [HttpPost("don/{id:long}/duyet", Name = "tienDoDuyetDon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DuyetDon(long id, DuyetDonTienDoRequest request, CancellationToken ct)
    {
        await service.DuyetDonAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("thong-bao", Name = "thongBaoDanhSach")]
    public Task<IReadOnlyList<ThongBaoItem>> ThongBao(CancellationToken ct) => service.ThongBaoCuaToiAsync(ct);

    [HttpPost("thong-bao/{id:long}/doc", Name = "thongBaoDaDoc")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DaDoc(long id, CancellationToken ct)
    {
        await service.DanhDauDocAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Thao tác trên hồ sơ SV: phiếu giao nhiệm vụ, nộp báo cáo, đơn, tạm dừng.</summary>
[ApiController]
[Route("api/v1/svtrongdot")]
[Tags("TienDo")]
public sealed class TienDoHoSoController(ITienDoService service) : ControllerBase
{
    [HttpGet("{id:long}/phieu-giao-nhiem-vu", Name = "phieuGiaoLay")]
    public Task<PhieuGiaoNhiemVuItem?> LayPhieu(long id, CancellationToken ct) => service.LayPhieuAsync(id, ct);

    /// <summary>GVHD (còn hiệu lực) lập / sửa phiếu giao nhiệm vụ của TỪNG sinh viên (nhóm có phiếu riêng).</summary>
    [HttpPut("{id:long}/phieu-giao-nhiem-vu", Name = "phieuGiaoLuu")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<PhieuGiaoNhiemVuItem> LuuPhieu(long id, PhieuGiaoNhiemVuRequest request, CancellationToken ct) =>
        service.LuuPhieuAsync(id, request, ct);

    [HttpGet("{id:long}/phieu-giao-nhiem-vu/pdf", Name = "phieuGiaoPdf")]
    public async Task<IActionResult> PhieuPdf(long id, CancellationToken ct)
    {
        var (noi, ten) = await service.XuatPhieuPdfAsync(id, ct);
        return File(noi, "application/pdf", ten);
    }

    /// <summary>SV nộp báo cáo tiến độ: file (PDF/DOCX) + % hoàn thành + cam kết AI (C07).</summary>
    [HttpPost("{id:long}/bao-cao", Name = "baoCaoTienDoNop")]
    [CoQuyen(Quyen.HoSoCuaToi)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaoCaoTienDoItem>> NopBaoCao(long id, [FromForm] NopBaoCaoForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return Problem(title: "Thiếu file báo cáo.", statusCode: StatusCodes.Status400BadRequest);
        await using var stream = form.File.OpenReadStream();
        var item = await service.NopBaoCaoAsync(id, form.MocTienDoId, stream, form.File.FileName,
            form.File.ContentType ?? "application/octet-stream", new NopBaoCaoRequest(form.TyLeHoanThanhPct, form.CamKetAI), ct);
        return Ok(item);
    }

    /// <summary>SV xin gia hạn (GiaHan) hoặc xin dừng (XinDung) qua DonTu.</summary>
    [HttpPost("{id:long}/don-tien-do", Name = "donTienDoTao")]
    [CoQuyen(Quyen.HoSoCuaToi)]
    public Task<DonTienDoItem> TaoDon(long id, DonTienDoTaoRequest request, CancellationToken ct) =>
        service.TaoDonAsync(id, request, ct);

    /// <summary>Phòng ĐT chuyển tạm dừng (T05) khi SV nghỉ học tạm thời (C03).</summary>
    [HttpPost("{id:long}/tam-dung", Name = "tienDoTamDung")]
    [CoQuyen(Quyen.DotQuanLy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TamDung(long id, [FromBody] string lyDo, CancellationToken ct)
    {
        await service.TamDungAsync(id, lyDo, ct);
        return NoContent();
    }
}

/// <summary>GVHD nhận xét báo cáo và chấm điểm tiến độ.</summary>
[ApiController]
[Route("api/v1/bao-cao-tien-do")]
[Tags("TienDo")]
public sealed class BaoCaoTienDoController(ITienDoService service) : ControllerBase
{
    [HttpPost("{id:long}/nhan-xet", Name = "baoCaoNhanXet")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<BaoCaoTienDoItem> NhanXet(long id, NhanXetBaoCaoRequest request, CancellationToken ct) =>
        service.NhanXetBaoCaoAsync(id, request, ct);
}
