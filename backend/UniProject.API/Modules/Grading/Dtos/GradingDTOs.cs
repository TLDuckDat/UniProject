namespace UniProject.API.Modules.Grading
{
    public record EvaluationScoreDto(
        int Id,
        int ProjectId,
        int StudentId,
        int LecturerId,
        int? CommitteeId,
        string GradingType,
        decimal Score,
        string? Comments,
        DateTime GradedDate);

    // Giảng viên chấm và hội đồng được xác định từ người đăng nhập / lịch bảo vệ, không nhận từ client.
    // StudentId bắt buộc khi loại đồ án chấm riêng; bỏ trống được khi chấm chung cả nhóm.
    public record SubmitGradeRequest(
        int? StudentId,
        string GradingType,
        decimal Score,
        string? Comments);

    public record FinalResultDto(
        int Id,
        int ProjectId,
        int StudentId,
        decimal? AdvisorScore,
        decimal? ReviewerScore,
        decimal? CommitteeScore,
        decimal? FinalScore,
        string Rank,
        string Result,
        DateTime? PublishedDate);
}
