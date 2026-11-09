using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.Abstractions;
using Upm.Application.Common;
using Upm.Application.Workflow;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

/// <summary>UC-1 · Đợt ĐATN và xét điều kiện giao (Phòng ĐT).</summary>
[ApiController]
[Route("api/v1/dot-datn")]
[Tags("DotDATN")]
public sealed class DotDATNController(IDotDATNService service, IXetGiaoJobScheduler jobScheduler, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet(Name = "dotDatnDanhSach")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<PagedResult<DotDATNItem>> DanhSach(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        service.DanhSachDotAsync(q, page, pageSize, ct);

    [HttpGet("{id:long}", Name = "dotDatnLay")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<DotDATNItem> Lay(long id, CancellationToken ct) => service.LayDotAsync(id, ct);

    [HttpPost(Name = "dotDatnTao")]
    [CoQuyen(Quyen.DotQuanLy)]
    public async Task<ActionResult<DotDATNItem>> Tao(DotDATNRequest request, CancellationToken ct)
    {
        var result = await service.TaoDotAsync(request, ct);
        return CreatedAtRoute("dotDatnLay", new { id = result.Id }, result);
    }

    [HttpPut("{id:long}", Name = "dotDatnSua")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<DotDATNItem> Sua(long id, DotDATNRequest request, CancellationToken ct) => service.SuaDotAsync(id, request, ct);

    [HttpPost("{id:long}/dong", Name = "dotDatnDong")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<DotDATNItem> Dong(long id, CancellationToken ct) => service.DongDotAsync(id, ct);

    [HttpGet("{id:long}/sinh-vien-ung-vien", Name = "dotDatnSinhVienUngVien")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<IReadOnlyList<SinhVienUngVienItem>> SinhVienUngVien(
        long id, [FromQuery] int? khoaHoc, [FromQuery] string? nganh, [FromQuery] string? q, CancellationToken ct) =>
        service.TimSinhVienUngVienAsync(id, khoaHoc, nganh, q, ct);

    [HttpGet("{id:long}/sinh-vien", Name = "dotDatnHoSo")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<DanhSachHoSoKetQua> HoSo(
        long id, [FromQuery] TrangThaiSVTrongDot? trangThai, [FromQuery] bool? coNoTinChi, [FromQuery] bool? chuaQuaTTTN,
        [FromQuery] string? ketQuaXet, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        service.DanhSachHoSoAsync(id, new LocHoSoRequest(trangThai, coNoTinChi, chuaQuaTTTN, ketQuaXet, q, page, pageSize), ct);

    [HttpPost("{id:long}/sinh-vien", Name = "dotDatnThemHoSo")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<ThemHoSoKetQua> ThemHoSo(long id, IReadOnlyList<HoSoTrongDotRequest> items, CancellationToken ct) =>
        service.ThemHoSoAsync(id, items, ct);

    [HttpPost("{id:long}/sinh-vien/theo-loc", Name = "dotDatnThemHoSoTheoLoc")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<ThemHoSoKetQua> ThemHoSoTheoLoc(long id, ThemHoSoTheoLocRequest request, CancellationToken ct) =>
        service.ThemHoSoTheoLocAsync(id, request, ct);

    /// <summary>Đưa job xét giao của đợt vào hàng đợi (Hangfire). 422 <c>UPM_XET_GIAO_DANG_CHAY</c> nếu lượt trước chưa xong.</summary>
    [HttpPost("{id:long}/xet-giao/chay", Name = "dotDatnChayXetGiao")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    [ProducesResponseType<XetGiaoJobItem>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<XetGiaoJobItem>> ChayXetGiao(long id, CancellationToken ct)
    {
        _ = await service.LayDotAsync(id, ct); // kiểm tra phạm vi trước khi đưa job vào hàng đợi
        var job = jobScheduler.DuaVaoHangDoi(id, currentUser.TaiKhoanId);
        return AcceptedAtRoute("dotDatnTienDoXetGiao", new { id, jobId = job.JobId }, job);
    }

    [HttpGet("{id:long}/xet-giao/jobs/{jobId}", Name = "dotDatnTienDoXetGiao")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    public async Task<ActionResult<XetGiaoJobItem>> TienDoXetGiao(long id, string jobId, CancellationToken ct)
    {
        _ = await service.LayDotAsync(id, ct);
        return jobScheduler.Lay(id, jobId) is { } job ? job : NotFound();
    }

    /// <summary>Lượt xét gần nhất của đợt (204 nếu chưa chạy) – để trang chi tiết đợt khôi phục thanh tiến độ khi tải lại.</summary>
    [HttpGet("{id:long}/xet-giao/jobs/gan-nhat", Name = "dotDatnXetGiaoGanNhat")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    [ProducesResponseType<XetGiaoJobItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<XetGiaoJobItem>> XetGiaoGanNhat(long id, CancellationToken ct)
    {
        _ = await service.LayDotAsync(id, ct);
        return jobScheduler.LayGanNhat(id) is { } job ? job : NoContent();
    }

    [HttpGet("{id:long}/khieu-nai", Name = "dotDatnKhieuNai")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    public Task<IReadOnlyList<KhieuNaiItem>> KhieuNai(long id, [FromQuery] TrangThaiDonTu? trangThai, CancellationToken ct) =>
        service.DanhSachKhieuNaiAsync(id, trangThai, ct);

    [HttpGet("{id:long}/quyet-dinh-giao", Name = "dotDatnQuyetDinhGiao")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<IReadOnlyList<QuyetDinhGiaoItem>> QuyetDinhGiao(long id, CancellationToken ct) =>
        service.DanhSachQuyetDinhGiaoAsync(id, ct);

    /// <summary>Ghi nhận QĐ giao ĐATN → T03 cho các hồ sơ đủ ĐK đã có đề tài; khóa dữ liệu (DaKhoaDuLieu).</summary>
    [HttpPost("{id:long}/quyet-dinh-giao", Name = "dotDatnGhiNhanQuyetDinhGiao")]
    [CoQuyen(Quyen.QuyetDinhGhiNhan)]
    public Task<QuyetDinhGiaoItem> GhiNhanQuyetDinh(long id, QuyetDinhGiaoRequest request, CancellationToken ct) =>
        service.GhiNhanQuyetDinhAsync(id, request, ct);
}

/// <summary>Kết quả xét giao của một hồ sơ (Phòng ĐT) – snapshot, xét lại (A19).</summary>
[ApiController]
[Route("api/v1/svtrongdot")]
[Tags("XetGiao")]
public sealed class XetGiaoController(IDotDATNService service) : ControllerBase
{
    [HttpGet("{id:long}/xet-giao", Name = "xetGiaoSnapshot")]
    [CoQuyen(Quyen.DotQuanLy)]
    public Task<IReadOnlyList<XetGiaoSnapshotItem>> Snapshot(long id, CancellationToken ct) => service.LaySnapshotAsync(id, ct);

    [HttpPost("{id:long}/xet-lai", Name = "xetGiaoXetLai")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    public Task<XetGiaoSnapshotItem> XetLai(long id, CancellationToken ct) => service.XetLaiAsync(id, ct);
}

/// <summary>Sinh viên: kết quả xét giao ĐATN của tôi + khiếu nại (A20).</summary>
[ApiController]
[Route("api/v1/ho-so-cua-toi")]
[Tags("HoSoCuaToi")]
[CoQuyen(Quyen.HoSoCuaToi)]
public sealed class HoSoCuaToiController(IDotDATNService service) : ControllerBase
{
    [HttpGet("xet-giao", Name = "hoSoCuaToiXetGiao")]
    public Task<IReadOnlyList<HoSoCuaToiXetGiaoItem>> XetGiao(CancellationToken ct) => service.LayKetQuaCuaToiAsync(ct);

    [HttpPost("{svTrongDotId:long}/khieu-nai-xet-giao", Name = "hoSoCuaToiKhieuNaiXetGiao")]
    [ProducesResponseType<DonKhieuNaiKetQua>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DonKhieuNaiKetQua>> KhieuNai(long svTrongDotId, DonKhieuNaiRequest request, CancellationToken ct)
    {
        var id = await service.TaoKhieuNaiAsync(svTrongDotId, request, ct);
        return StatusCode(StatusCodes.Status201Created, new DonKhieuNaiKetQua(id));
    }
}

/// <summary>Phòng ĐT xử lý khiếu nại kết quả xét giao (A20): chấp nhận → xét lại; từ chối → bắt buộc lý do.</summary>
[ApiController]
[Route("api/v1/don-tu")]
[Tags("DonTu")]
public sealed class KhieuNaiXetGiaoController(IDotDATNService service) : ControllerBase
{
    [HttpPost("{id:long}/xu-ly-khieu-nai-xet-giao", Name = "donTuXuLyKhieuNaiXetGiao")]
    [CoQuyen(Quyen.XetDieuKienChay)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> XuLy(long id, XuLyKhieuNaiRequest request, CancellationToken ct)
    {
        await service.XuLyKhieuNaiAsync(id, request, ct);
        return NoContent();
    }
}
