using Microsoft.AspNetCore.Http;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Project
{
    public interface IProjectService
    {
        Task<PagedResult<ProjectDto>> GetProjectsAsync(PaginationParams @params, int? periodId = null, int? lecturerId = null, int? studentId = null, string? status = null, CancellationToken cancellationToken = default);
        Task<ProjectDto> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
        Task<Result> AssignAdvisorAsync(int projectId, AssignAdvisorRequest request, CancellationToken cancellationToken = default);
        Task<Result> RemoveAdvisorAsync(int projectId, int advisorAssignmentId, CancellationToken cancellationToken = default);

        Task<ProgressReportDto> CreateProgressReportAsync(int projectId, CreateProgressReportRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProgressReportDto>> GetProgressReportsAsync(int projectId, CancellationToken cancellationToken = default);
        Task<Result> ReviewProgressReportAsync(int reportId, CreateProgressReviewRequest request, CancellationToken cancellationToken = default);

        Task<RevisionRequestDto> CreateRevisionRequestAsync(int projectId, CreateRevisionRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateRevisionStatusAsync(int revisionId, UpdateRevisionStatusRequest request, CancellationToken cancellationToken = default);

        Task<ProjectDocumentDto> UploadDocumentAsync(int projectId, UploadDocumentRequest request, IFormFile file, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProjectDocumentDto>> GetDocumentsAsync(int projectId, CancellationToken cancellationToken = default);
        Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int projectId, int documentId, CancellationToken cancellationToken = default);
    }
}
