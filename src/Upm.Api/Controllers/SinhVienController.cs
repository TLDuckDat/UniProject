using Microsoft.AspNetCore.Mvc;
using Upm.Api.Authorization;
using Upm.Application.Common;
using Upm.Application.TraCuuSinhVien;
using Upm.Domain.PhanQuyen;

namespace Upm.Api.Controllers;

/// <summary>Tra cứu sinh viên (chỉ đọc) – kết quả tự lọc theo phạm vi cơ sở/khoa của người dùng.</summary>
[ApiController]
[Route("api/v1/sinh-vien")]
[Tags("SinhVien")]
public sealed class SinhVienController(ISinhVienQueries queries) : ControllerBase
{
    /// <summary>Danh sách sinh viên trong phạm vi được phép, có tìm kiếm theo mã/họ tên.</summary>
    [HttpGet(Name = "danhSachSinhVien")]
    [CoQuyen(Quyen.SinhVienXem)]
    [ProducesResponseType<PagedResult<SinhVienListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SinhVienListItem>>> DanhSach(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await queries.DanhSachAsync(q, new PageRequest(page, pageSize), ct));
}
