namespace UniProject.API.Modules.ProjectPeriod
{
    /// <summary>Luật của một loại đồ án theo cấu hình đang chạy, để giao diện hiển thị. MinComponentScore = null nghĩa là đã tắt.</summary>
    public record ProjectTypeDto(
        string Kind,
        string Name,
        int MaxMembers,
        bool RequiresReviewer,
        decimal AdvisorWeight,
        decimal ReviewerWeight,
        decimal CommitteeWeight,
        decimal PassingScore,
        decimal? MinComponentScore,
        string GradingMode);

    public record ProjectPeriodDto(
        int Id,
        string Code,
        string Name,
        string ProjectKind,
        string ProjectKindName,
        int SemesterId,
        DateTime StartDate,
        DateTime EndDate,
        DateTime RegistrationDeadline,
        DateTime OutlineDeadline,
        DateTime ReportDeadline,
        DateTime DefenseStartDate,
        DateTime DefenseEndDate,
        string Status,
        bool IsActive);

    public record CreateProjectPeriodRequest(
        string Code,
        string Name,
        string ProjectKind,
        int SemesterId,
        DateTime StartDate,
        DateTime EndDate,
        DateTime RegistrationDeadline,
        DateTime OutlineDeadline,
        DateTime ReportDeadline,
        DateTime DefenseStartDate,
        DateTime DefenseEndDate);

    // Loại đồ án của đợt cố định sau khi tạo, vì đề tài, đơn đăng ký và đồ án trong đợt đều phụ thuộc vào nó.
    public record UpdateProjectPeriodRequest(
        string Name,
        int SemesterId,
        DateTime StartDate,
        DateTime EndDate,
        DateTime RegistrationDeadline,
        DateTime OutlineDeadline,
        DateTime ReportDeadline,
        DateTime DefenseStartDate,
        DateTime DefenseEndDate);

    public record ChangePeriodStatusRequest(string Status);

    public record ProjectPeriodClassDto(int Id, int ProjectPeriodId, int ClassId);
}
