using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Defense
{
    public interface IDefenseService
    {
        Task<Result> AssignReviewerAsync(int projectId, AssignReviewerRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReviewerAssignmentDto>> GetReviewersAsync(int projectId, CancellationToken cancellationToken = default);

        Task<PagedResult<CommitteeDto>> GetCommitteesAsync(PaginationParams @params, int? periodId = null, CancellationToken cancellationToken = default);
        Task<CommitteeDto> GetCommitteeByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<CommitteeDto> CreateCommitteeAsync(CreateCommitteeRequest request, CancellationToken cancellationToken = default);
        Task<Result> AddCommitteeMemberAsync(int committeeId, AddCommitteeMemberRequest request, CancellationToken cancellationToken = default);
        Task<Result> RemoveCommitteeMemberAsync(int committeeId, int memberId, CancellationToken cancellationToken = default);

        Task<DefenseScheduleDto> CreateDefenseScheduleAsync(CreateDefenseScheduleRequest request, CancellationToken cancellationToken = default);
        Task<DefenseScheduleDto> UpdateDefenseScheduleAsync(int id, UpdateDefenseScheduleRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteDefenseScheduleAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DefenseScheduleDto>> GetDefenseSchedulesAsync(int? periodId = null, int? committeeId = null, int? projectId = null, DateTime? date = null, CancellationToken cancellationToken = default);
    }
}
