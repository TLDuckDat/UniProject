namespace UniProject.API.Modules.ProjectPeriod
{
    /// <summary>Trọng số các cột điểm; tổng luôn bằng 1. Trọng số 0 nghĩa là loại đồ án không có cột điểm đó.</summary>
    public record GradeWeights(decimal Advisor, decimal Reviewer, decimal Committee);

    /// <summary>
    /// Ngưỡng đạt: điểm tổng kết tối thiểu, và mức tối thiểu của từng cột bắt buộc (null = không áp dụng).
    /// </summary>
    public record PassRules(decimal FinalPassMark, decimal? ComponentPassMark)
    {
        public static PassRules Default { get; } = new(5.0m, 4.0m);
    }

    /// <summary>
    /// Strategy: tập luật khác nhau giữa Đồ án cơ sở và Đồ án chuyên ngành.
    /// Service nghiệp vụ không viết if/else theo loại đồ án mà hỏi policy (lấy qua ProjectTypePolicyResolver).
    /// </summary>
    public interface IProjectTypePolicy
    {
        ProjectKind Kind { get; }
        string DisplayName { get; }

        /// <summary>Số sinh viên tối đa trong một nhóm / một đồ án.</summary>
        int MaxMembers { get; }

        /// <summary>Có phân công giảng viên phản biện và chấm điểm phản biện hay không.</summary>
        bool RequiresReviewer { get; }

        GradeWeights Weights { get; }

        PassRules PassRules { get; }

        /// <summary>Chấm riêng từng sinh viên hay chấm chung cả nhóm.</summary>
        GradingMode GradingMode { get; }

        /// <summary>Kiểm tra điều kiện tiên quyết để sinh viên được tham gia loại đồ án này.</summary>
        Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default);
    }
}
