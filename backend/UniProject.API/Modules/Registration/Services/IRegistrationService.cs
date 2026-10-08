using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Registration
{
    public interface IRegistrationService
    {
        Task<RegistrationDto> CreateRegistrationAsync(CreateRegistrationRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<RegistrationDto>> GetRegistrationsAsync(PaginationParams @params, int? periodId = null, string? status = null, CancellationToken cancellationToken = default);
        Task<RegistrationDto> GetRegistrationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<RegistrationDto?> GetMyRegistrationAsync(int? periodId = null, CancellationToken cancellationToken = default);
        Task<Result> AddMemberAsync(int registrationId, AddMemberRequest request, CancellationToken cancellationToken = default);
        Task<Result> RemoveMemberAsync(int registrationId, int studentId, CancellationToken cancellationToken = default);
        Task<Result> AddPreferenceAsync(int registrationId, AddPreferenceRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdatePreferenceOrderAsync(int registrationId, UpdatePreferenceOrderRequest request, CancellationToken cancellationToken = default);
        Task<Result> SubmitRegistrationAsync(int registrationId, CancellationToken cancellationToken = default);
        Task<Result> CancelRegistrationAsync(int registrationId, CancellationToken cancellationToken = default);
        Task<Result> ApprovePreferenceAsync(int preferenceId, CancellationToken cancellationToken = default);
        Task<Result> RejectPreferenceAsync(int preferenceId, RejectPreferenceRequest request, CancellationToken cancellationToken = default);
    }
}
