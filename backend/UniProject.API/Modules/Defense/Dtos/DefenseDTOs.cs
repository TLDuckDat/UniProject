namespace UniProject.API.Modules.Defense
{
    public record ReviewerAssignmentDto(int Id, int ProjectId, int LecturerId, DateTime AssignedDate, string Status);

    public record CommitteeMemberDto(int Id, int LecturerId, string Role);

    public record CommitteeDto(
        int Id,
        string Code,
        string Name,
        int PeriodId,
        string? Location,
        string Status,
        IReadOnlyList<CommitteeMemberDto> Members,
        bool IsReadyForDefense,
        IReadOnlyList<string> CompositionErrors);

    public record DefenseScheduleDto(
        int Id,
        int ProjectId,
        int CommitteeId,
        int PeriodId,
        DateTime DefenseDate,
        TimeSpan StartTime,
        TimeSpan EndTime,
        string Room,
        string Status);

    public record AssignReviewerRequest(int LecturerId);

    public record CreateCommitteeRequest(string Code, string Name, int PeriodId, string? Location);

    public record AddCommitteeMemberRequest(int LecturerId, string Role = "UY_VIEN");

    public record CreateDefenseScheduleRequest(
        int ProjectId,
        int CommitteeId,
        int PeriodId,
        DateTime DefenseDate,
        TimeSpan StartTime,
        TimeSpan EndTime,
        string Room);

    public record UpdateDefenseScheduleRequest(
        DateTime DefenseDate,
        TimeSpan StartTime,
        TimeSpan EndTime,
        string Room);
}
