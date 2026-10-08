using UniProject.Share.Domain;

namespace UniProject.API.Modules.Grading
{
    public interface IGradingService
    {
        Task<IReadOnlyList<EvaluationScoreDto>> SubmitGradeAsync(int projectId, SubmitGradeRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EvaluationScoreDto>> GetProjectGradesAsync(int projectId, CancellationToken cancellationToken = default);
        Task<Result> PublishResultAsync(int projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FinalResultDto>> GetProjectResultsAsync(int projectId, CancellationToken cancellationToken = default);
    }
}
