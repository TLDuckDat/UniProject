namespace UniProject.API.Modules.ProjectPeriod
{
    /// <summary>
    /// Các loại đồ án hệ thống quản lý. Mỗi đợt đồ án và mỗi đề tài thuộc đúng một loại;
    /// luật riêng của từng loại nằm trong các lớp IProjectTypePolicy tương ứng.
    /// </summary>
    public enum ProjectKind
    {
        DO_AN_CO_SO,
        DO_AN_CHUYEN_NGANH,
        /// <summary>Đồ án của một học phần (vd Công nghệ Java, Lập trình trực quan).</summary>
        DO_AN_MON_HOC,
        /// <summary>Đồ án tốt nghiệp: mỗi sinh viên một đề tài, có giảng viên đọc duyệt.</summary>
        DO_AN_TOT_NGHIEP
    }

    public static class ProjectKindExtensions
    {
        public static string ToDisplayName(this ProjectKind kind) => kind switch
        {
            ProjectKind.DO_AN_CO_SO => "Đồ án cơ sở",
            ProjectKind.DO_AN_CHUYEN_NGANH => "Đồ án chuyên ngành",
            ProjectKind.DO_AN_MON_HOC => "Đồ án môn học",
            ProjectKind.DO_AN_TOT_NGHIEP => "Đồ án tốt nghiệp",
            _ => kind.ToString()
        };
    }
}
