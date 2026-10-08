using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.ProjectPeriod
{
    public interface IProjectPeriodService
    {
        Task<IReadOnlyList<ProjectTypeDto>> GetProjectTypesAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<ProjectPeriodDto>> GetProjectPeriodsAsync(PaginationParams @params, int? semesterId = null, string? status = null, string? projectKind = null, CancellationToken cancellationToken = default);
        Task<ProjectPeriodDto> GetProjectPeriodByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ProjectPeriodDto> CreateProjectPeriodAsync(CreateProjectPeriodRequest request, CancellationToken cancellationToken = default);
        Task<ProjectPeriodDto> UpdateProjectPeriodAsync(int id, UpdateProjectPeriodRequest request, CancellationToken cancellationToken = default);
        Task<Result> ChangeStatusAsync(int id, ChangePeriodStatusRequest request, CancellationToken cancellationToken = default);
        Task<Result> AddClassToPeriodAsync(int periodId, int classId, CancellationToken cancellationToken = default);
        Task<Result> RemoveClassFromPeriodAsync(int periodId, int classId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<int>> GetClassesInPeriodAsync(int periodId, CancellationToken cancellationToken = default);
    }
}
