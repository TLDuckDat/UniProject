using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Topic
{
    public interface ITopicService
    {
        Task<PagedResult<TopicDto>> GetTopicsAsync(PaginationParams @params, int? lecturerId = null, int? majorId = null, string? status = null, string? projectKind = null, CancellationToken cancellationToken = default);
        Task<TopicDto> GetTopicByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TopicDto> ProposeTopicByStudentAsync(StudentProposeTopicRequest request, CancellationToken cancellationToken = default);
        Task<TopicDto> CreateTopicAsync(CreateTopicRequest request, CancellationToken cancellationToken = default);
        Task<TopicDto> UpdateTopicAsync(int id, UpdateTopicRequest request, CancellationToken cancellationToken = default);
        Task<Result> ApproveTopicAsync(int id, CancellationToken cancellationToken = default);
        Task<Result> RejectTopicAsync(int id, RejectTopicRequest request, CancellationToken cancellationToken = default);
        Task<Result> DisableTopicAsync(int id, CancellationToken cancellationToken = default);

        Task<Result> AddTopicToPeriodAsync(AddTopicToPeriodRequest request, CancellationToken cancellationToken = default);
        Task<Result> ChangePeriodTopicStatusAsync(int periodTopicId, ChangePeriodTopicStatusRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PeriodTopicDto>> GetTopicsInPeriodAsync(int periodId, string? status = null, CancellationToken cancellationToken = default);
    }
}
