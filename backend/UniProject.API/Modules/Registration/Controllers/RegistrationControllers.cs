using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Registration
{
    [ApiController]
    [Route("api/v1/registrations")]
    [Tags("Registrations")]
    public class RegistrationsController : ControllerBase
    {
        private readonly IRegistrationService _service;

        public RegistrationsController(IRegistrationService service)
        {
            _service = service;
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<RegistrationDto>), 200)]
        public async Task<IActionResult> Get(
            [FromQuery] PaginationParams @params,
            [FromQuery] int? periodId,
            [FromQuery] string? status,
            CancellationToken ct)
        {
            var result = await _service.GetRegistrationsAsync(@params, periodId, status, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Result<RegistrationDto>), 200)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetRegistrationByIdAsync(id, ct);
            return Ok(Result<RegistrationDto>.Ok(result));
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<RegistrationDto?>), 200)]
        public async Task<IActionResult> GetMyRegistration([FromQuery] int? periodId, CancellationToken ct)
        {
            var result = await _service.GetMyRegistrationAsync(periodId, ct);
            return Ok(Result<RegistrationDto?>.Ok(result));
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost]
        [ProducesResponseType(typeof(Result<RegistrationDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request, CancellationToken ct)
        {
            var result = await _service.CreateRegistrationAsync(request, ct);
            return StatusCode(201, Result<RegistrationDto>.Ok(result, "Tạo đơn đăng ký nhóm thành công."));
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("{id:int}/members")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest request, CancellationToken ct)
        {
            var result = await _service.AddMemberAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpDelete("{id:int}/members/{studentId:int}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> RemoveMember(int id, int studentId, CancellationToken ct)
        {
            var result = await _service.RemoveMemberAsync(id, studentId, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("{id:int}/preferences")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> AddPreference(int id, [FromBody] AddPreferenceRequest request, CancellationToken ct)
        {
            var result = await _service.AddPreferenceAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPut("{id:int}/preferences/order")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> UpdatePreferenceOrder(int id, [FromBody] UpdatePreferenceOrderRequest request, CancellationToken ct)
        {
            var result = await _service.UpdatePreferenceOrderAsync(id, request, ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost("{id:int}/submit")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Submit(int id, CancellationToken ct)
        {
            var result = await _service.SubmitRegistrationAsync(id, ct);
            return Ok(result);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SinhVien}")]
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            var result = await _service.CancelRegistrationAsync(id, ct);
            return Ok(result);
        }
    }

    [ApiController]
    [Route("api/v1/registration-preferences")]
    [Tags("Registration Preferences")]
    public class RegistrationPreferencesController : ControllerBase
    {
        private readonly IRegistrationService _service;

        public RegistrationPreferencesController(IRegistrationService service)
        {
            _service = service;
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Approve(int id, CancellationToken ct)
        {
            var result = await _service.ApprovePreferenceAsync(id, ct);
            return Ok(result);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.GiangVien}")]
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectPreferenceRequest request, CancellationToken ct)
        {
            var result = await _service.RejectPreferenceAsync(id, request, ct);
            return Ok(result);
        }
    }

    /// <summary>Yêu cầu đổi đề tài / đổi nhóm sau khi đơn đăng ký đã duyệt: sinh viên gửi, Admin duyệt hoặc từ chối.</summary>
    [ApiController]
    [Route("api/v1/registration-change-requests")]
    [Tags("Registration Change Requests")]
    public class RegistrationChangeRequestsController : ControllerBase
    {
        private readonly IRegistrationChangeService _service;

        public RegistrationChangeRequestsController(IRegistrationChangeService service)
        {
            _service = service;
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpPost]
        [ProducesResponseType(typeof(Result<ChangeRequestDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateChangeRequestRequest request, CancellationToken ct)
        {
            var result = await _service.CreateAsync(request, ct);
            return StatusCode(201, Result<ChangeRequestDto>.Ok(result, "Gửi yêu cầu thay đổi thành công. Chờ duyệt."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ChangeRequestDto>>), 200)]
        public async Task<IActionResult> Get([FromQuery] int? periodId, [FromQuery] string? status, CancellationToken ct)
        {
            var result = await _service.GetAsync(periodId, status, ct);
            return Ok(Result<IReadOnlyList<ChangeRequestDto>>.Ok(result));
        }

        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<IReadOnlyList<ChangeRequestDto>>), 200)]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var result = await _service.GetMineAsync(ct);
            return Ok(Result<IReadOnlyList<ChangeRequestDto>>.Ok(result));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveChangeRequestRequest? request, CancellationToken ct)
        {
            var result = await _service.ApproveAsync(id, request ?? new ApproveChangeRequestRequest(null), ct);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectChangeRequestRequest request, CancellationToken ct)
        {
            var result = await _service.RejectAsync(id, request, ct);
            return Ok(result);
        }
    }
}
