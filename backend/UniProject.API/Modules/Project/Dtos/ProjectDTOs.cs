namespace UniProject.API.Modules.Project
{
    public record ProjectMemberDto(int Id, int StudentId, string Role);

    public record AdvisorAssignmentDto(int Id, int LecturerId, string AdvisorType, DateTime AssignedDate, bool IsActive);

    public record ProgressReviewDto(int Id, int ReportId, int LecturerId, string ReviewContent, string Evaluation, DateTime ReviewedDate);

    public record ProgressReportDto(
        int Id,
        int ProjectId,
        int SubmittedByStudentId,
        int ReportNumber,
        string Title,
        string Content,
        string? WorkDone,
        string? NextTasks,
        string? Difficulties,
        int CompletionPercentage,
        DateTime SubmittedDate,
        IReadOnlyList<ProgressReviewDto> Reviews);

    public record RevisionRequestDto(
        int Id,
        int ProjectId,
        int RequestedByLecturerId,
        string Title,
        string Content,
        DateTime? Deadline,
        DateTime RequestedDate,
        string Status);

    public record ProjectDocumentDto(
        int Id,
        int ProjectId,
        int UploadedByStudentId,
        string DocumentName,
        string FilePath,
        string DocumentType,
        int Version,
        DateTime UploadedDate);

    public record ProjectDto(
        int Id,
        string Code,
        string Title,
        int PeriodId,
        int TopicId,
        int? RegistrationId,
        DateTime CreatedDate,
        DateTime? StartDate,
        DateTime? EndDate,
        string Status,
        IReadOnlyList<ProjectMemberDto> Members,
        IReadOnlyList<AdvisorAssignmentDto> Advisors);

    public record CreateProjectRequest(
        string Code,
        string Title,
        int PeriodId,
        int TopicId,
        int? RegistrationId,
        IReadOnlyList<CreateProjectMemberDto> Members,
        int? PrimaryAdvisorLecturerId);

    public record CreateProjectMemberDto(int StudentId, string Role = "THANH_VIEN");

    public record AssignAdvisorRequest(int LecturerId, string AdvisorType = "CHINH");

    public record CreateProgressReportRequest(
        int ReportNumber,
        string Title,
        string Content,
        string? WorkDone,
        string? NextTasks,
        string? Difficulties,
        int CompletionPercentage);

    public record CreateProgressReviewRequest(
        string ReviewContent,
        string Evaluation = "DAT");

    public record CreateRevisionRequest(
        string Title,
        string Content,
        DateTime? Deadline);

    public record UpdateRevisionStatusRequest(string Status);

    public record UploadDocumentRequest(
        string DocumentName,
        string DocumentType,
        int Version = 1);
}
