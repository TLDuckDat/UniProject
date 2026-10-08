namespace UniProject.API.Modules.Topic
{
    public record TopicDto(
        int Id,
        string Code,
        string Title,
        string Description,
        string? Objectives,
        string? Requirements,
        string? ExpectedTechnologies,
        int MaxStudents,
        string ProjectKind,
        int ProposedByLecturerId,
        int? ProposedByStudentId,
        int MajorId,
        int? ApprovedByLecturerId,
        DateTime? ApprovedDate,
        string? RejectReason,
        string Status,
        DateTime CreatedAt,
        bool IsActive);

    public record PeriodTopicDto(
        int Id,
        int TopicId,
        string TopicCode,
        string TopicTitle,
        int MaxStudents,
        int PeriodId,
        string ProjectKind,
        string Status);

    public record CreateTopicRequest(
        string Code,
        string Title,
        string Description,
        string? Objectives,
        string? Requirements,
        string? ExpectedTechnologies,
        int MaxStudents,
        int MajorId,
        string ProjectKind,
        int? ProposedByLecturerId = null); // chỉ Admin dùng; giảng viên luôn đứng tên chính mình

    /// <summary>Sinh viên đề xuất đề tài; bắt buộc chọn giảng viên dự kiến hướng dẫn (người sẽ duyệt đề tài).</summary>
    public record StudentProposeTopicRequest(
        string Code,
        string Title,
        string Description,
        string? Objectives,
        string? Requirements,
        string? ExpectedTechnologies,
        int MaxStudents,
        int MajorId,
        string ProjectKind,
        int ExpectedAdvisorLecturerId);

    public record UpdateTopicRequest(
        string Title,
        string Description,
        string? Objectives,
        string? Requirements,
        string? ExpectedTechnologies,
        int MaxStudents,
        int MajorId);

    public record RejectTopicRequest(string Reason);

    public record AddTopicToPeriodRequest(int TopicId, int PeriodId, string? Status = "DANG_MO");

    public record ChangePeriodTopicStatusRequest(string Status);
}
