using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.DeTaiPhanCong;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

/// <summary>UC-2 · Đề tài và phân công (SV, GV, Trưởng BM).</summary>
[ApiController]
[Route("api/v1/de-tai")]
[Tags("DeTai")]
public sealed class DeTaiController(IDeTaiService service) : ControllerBase
{
    /// <summary>Danh mục đề tài của đợt. SV: chỉ đề tài đã duyệt; GV/BM: theo phạm vi dữ liệu.</summary>
    [HttpGet(Name = "deTaiDanhSach")]
    public Task<IReadOnlyList<DeTaiItem>> DanhSach(
        [FromQuery] long dotId, [FromQuery] long? boMonId, [FromQuery] TrangThaiDuyetDeTai? trangThai, [FromQuery] string? q,
        CancellationToken ct) => service.DanhSachAsync(dotId, boMonId, trangThai, q, ct);

    [HttpGet("cua-toi", Name = "deTaiCuaToi")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<DeTaiItem>> CuaToi([FromQuery] long dotId, CancellationToken ct) =>
        service.DeTaiCuaToiAsync(dotId, ct);

    /// <summary>GV đề xuất đề tài → Bộ môn duyệt. Trả về cảnh báo trùng (B10) nếu có.</summary>
    /// <summary>Các đợt người dùng được xem (bộ chọn đợt trên giao diện đề tài).</summary>
    [HttpGet("dot", Name = "deTaiDanhSachDot")]
    public Task<IReadOnlyList<DotTomTat>> DanhSachDot(CancellationToken ct) => service.DanhSachDotAsync(ct);

    /// <summary>Bộ môn của cơ sở đợt.</summary>
    [HttpGet("bo-mon", Name = "deTaiBoMon")]
    public Task<IReadOnlyList<BoMonTomTat>> BoMon([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachBoMonAsync(dotId, ct);

    /// <summary>GV đang hoạt động của cơ sở đợt – SV dùng để chọn GV đề nghị khi tự đề xuất.</summary>
    [HttpGet("gv-de-nghi", Name = "deTaiGvDeNghi")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<GiangVienTomTat>> GvDeNghi([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachGvDeNghiAsync(dotId, ct);

    [HttpPost(Name = "deTaiGvDeXuat")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public async Task<ActionResult<DeTaiItem>> GvDeXuat(DeTaiTaoRequest request, CancellationToken ct)
    {
        var item = await service.GvDeXuatAsync(request, ct);
        return CreatedAtRoute("deTaiDanhSach", new { dotId = item.DotId }, item);
    }

    /// <summary>SV tự đề xuất đề tài (B04): GV được đề nghị xác nhận, rồi Bộ môn duyệt.</summary>
    [HttpPost("sv-de-xuat", Name = "deTaiSvDeXuat")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<DeTaiItem> SvDeXuat(SvDeXuatRequest request, CancellationToken ct) => service.SvDeXuatAsync(request, ct);

    [HttpPost("{id:long}/gv-xac-nhan", Name = "deTaiGvXacNhan")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<DeTaiItem> GvXacNhan(long id, DuyetDeTaiRequest request, CancellationToken ct) =>
        service.GvXacNhanSvDeXuatAsync(id, request.DongY, ct);

    [HttpPost("{id:long}/duyet", Name = "deTaiBoMonDuyet")]
    [CoQuyen(Quyen.DeTaiDuyet)]
    public Task<DeTaiItem> BoMonDuyet(long id, DuyetDeTaiRequest request, CancellationToken ct) =>
        service.BoMonDuyetAsync(id, request.DongY, ct);

    [HttpGet("{id:long}/dang-ky", Name = "deTaiDanhSachDangKy")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<DangKyNguyenVongItem>> DanhSachDangKy(long id, CancellationToken ct) =>
        service.DanhSachDangKyAsync(id, ct);

    /// <summary>GV duyệt SV đăng ký nguyện vọng. Đủ MaxSV thì khóa đề tài.</summary>
    [HttpPost("{id:long}/duyet-dang-ky", Name = "deTaiGvDuyetDangKy")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<DangKyNguyenVongItem> GvDuyetDangKy(long id, DuyetDangKyRequest request, CancellationToken ct) =>
        service.GvDuyetDangKyAsync(id, request, ct);

    [HttpGet("phan-cong", Name = "deTaiPhanCongDot")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<PhanCongDotItem> PhanCongDot([FromQuery] long dotId, CancellationToken ct) => service.PhanCongDotAsync(dotId, ct);

    /// <summary>Gợi ý phân công (B05) – chỉ đề xuất, không ghi dữ liệu.</summary>
    [HttpGet("goi-y-phan-cong", Name = "deTaiGoiYPhanCong")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<GoiYPhanCongItem>> GoiY([FromQuery] long dotId, CancellationToken ct) =>
        service.GoiYPhanCongAsync(dotId, ct);

    [HttpPost("phan-cong", Name = "deTaiPhanCong")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<PhanCongHoSoItem> PhanCong(PhanCongRequest request, CancellationToken ct) => service.PhanCongAsync(request, ct);

    [HttpGet("don-dieu-chinh", Name = "deTaiDonDieuChinhDanhSach")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<DonDieuChinhItem>> DonDieuChinh([FromQuery] long dotId, CancellationToken ct) =>
        service.DanhSachDonDieuChinhAsync(dotId, ct);

    /// <summary>Trưởng BM duyệt đơn đổi GVHD (B08) hoặc đổi đề tài (B09): ghi QĐ điều chỉnh.</summary>
    [HttpPost("don-dieu-chinh/{id:long}/duyet", Name = "deTaiDuyetDonDieuChinh")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DuyetDonDieuChinh(long id, DuyetDonDieuChinhRequest request, CancellationToken ct)
    {
        await service.DuyetDonDieuChinhAsync(id, request, ct);
        return NoContent();
    }
}

/// <summary>Thao tác trên một hồ sơ SV (đăng ký nguyện vọng, lịch sử phân công, đơn điều chỉnh).</summary>
[ApiController]
[Route("api/v1/svtrongdot")]
[Tags("DeTai")]
public sealed class DeTaiHoSoController(IDeTaiService service) : ControllerBase
{
    [HttpGet("{id:long}/nguyen-vong", Name = "svNguyenVongLay")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<NguyenVongItem>> LayNguyenVong(long id, CancellationToken ct) => service.LayNguyenVongAsync(id, ct);

    /// <summary>SV đăng ký danh sách nguyện vọng (1–5, theo thứ tự ưu tiên).</summary>
    [HttpPost("{id:long}/nguyen-vong", Name = "svNguyenVongDangKy")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public Task<IReadOnlyList<NguyenVongItem>> DangKyNguyenVong(long id, NguyenVongRequest request, CancellationToken ct) =>
        service.DangKyNguyenVongAsync(id, request, ct);

    [HttpGet("{id:long}/lich-su-phan-cong", Name = "svLichSuPhanCong")]
    [CoQuyen(Quyen.PhanCongQuanLy)]
    public Task<IReadOnlyList<PhanCongLichSuItem>> LichSuPhanCong(long id, CancellationToken ct) =>
        service.LichSuPhanCongAsync(id, ct);

    /// <summary>SV xin đổi GVHD (DoiGVHD) hoặc đổi đề tài (DoiDeTai) sau khi có quyết định giao.</summary>
    [HttpPost("{id:long}/don-dieu-chinh", Name = "svDonDieuChinhTao")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    [ProducesResponseType<object>(StatusCodes.Status201Created)]
    public async Task<ActionResult> TaoDonDieuChinh(long id, DonDieuChinhTaoRequest request, CancellationToken ct)
    {
        var donId = await service.TaoDonDieuChinhAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, new { donTuId = donId });
    }
}

/// <summary>GV tự khai báo xung đột lợi ích với một SV (B07).</summary>
[ApiController]
[Route("api/v1/giang-vien")]
[Tags("DeTai")]
public sealed class XungDotController(IDeTaiService service) : ControllerBase
{
    [HttpPost("xung-dot", Name = "giangVienKhaiBaoXungDot")]
    [CoQuyen(Quyen.DeTaiDeXuat)]
    public async Task<ActionResult> KhaiBao(KhaiBaoXungDotRequest request, CancellationToken ct)
    {
        var id = await service.KhaiBaoXungDotAsync(request, ct);
        return Ok(new { id });
    }
}
