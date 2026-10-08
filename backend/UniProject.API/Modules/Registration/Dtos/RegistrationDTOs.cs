namespace UniProject.API.Modules.Registration
{
    public record RegistrationMemberDto(int Id, int StudentId, string Role, DateTime JoinedDate);

    public record RegistrationPreferenceDto(
        int Id,
        int PeriodTopicId,
        int PriorityOrder,
        string Status,
        int? ApprovedByLecturerId,
        DateTime? ApprovedDate,
        string? RejectReason);

    public record RegistrationDto(
        int Id,
        int PeriodId,
        string? GroupName,
        DateTime RegistrationDate,
        string Status,
        IReadOnlyList<RegistrationMemberDto> Members,
        IReadOnlyList<RegistrationPreferenceDto> Preferences);

    public record CreateRegistrationRequest(int PeriodId, string? GroupName);

    public record AddMemberRequest(int StudentId); // người được thêm luôn là thành viên; trưởng nhóm là người tạo đơn

    public record AddPreferenceRequest(int PeriodTopicId, int PriorityOrder);

    public record UpdatePreferenceOrderRequest(int PreferenceId, int NewPriorityOrder);

    public record RejectPreferenceRequest(string Reason);

    public record ChangeRequestDto(
        int Id,
        int RegistrationId,
        int StudentId,
        string Type,
        int? NewPeriodTopicId,
        int? TargetRegistrationId,
        string Reason,
        string Status,
        DateTime RequestedDate,
        DateTime? ProcessedDate,
        string? ProcessNote);

    /// <summary>
    /// Type = DOI_DE_TAI (trưởng nhóm, kèm NewPeriodTopicId) hoặc DOI_NHOM (sinh viên trong nhóm, kèm TargetRegistrationId).
    /// </summary>
    public record CreateChangeRequestRequest(int RegistrationId, string Type, int? NewPeriodTopicId, int? TargetRegistrationId, string Reason);

    public record ApproveChangeRequestRequest(string? Note);

    public record RejectChangeRequestRequest(string Reason);
}
