using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace UniProject.API.Modules.ProjectPeriod
{
    /// <summary>Cách nhập điểm của giảng viên cho một đồ án.</summary>
    public enum GradingMode
    {
        /// <summary>Chấm riêng từng sinh viên.</summary>
        CHAM_RIENG,
        /// <summary>Chấm chung cả nhóm: một lần nhập theo một vai trò áp dụng cho mọi thành viên.</summary>
        CHAM_CHUNG
    }

    /// <summary>
    /// Tham số của một loại đồ án, đọc từ cấu hình "ProjectTypes:&lt;LOAI_DO_AN&gt;:*".
    /// Quy chế đào tạo của trường chưa quy định riêng các tham số này cho đồ án cơ sở / chuyên ngành,
    /// nên bộ môn đổi được bằng cấu hình mà không phải sửa mã.
    /// </summary>
    public class ProjectTypeSettings
    {
        /// <summary>Số sinh viên tối đa trong nhóm (1 - 10, giới hạn trên theo CK_DETAI_SoLuongSinhVien).</summary>
        public int MaxMembers { get; set; }

        /// <summary>Có phân công giảng viên phản biện hay không.</summary>
        public bool HasReviewer { get; set; }

        /// <summary>Trọng số theo phần trăm; tổng ba cột phải bằng 100.</summary>
        public decimal AdvisorWeightPercent { get; set; }
        public decimal ReviewerWeightPercent { get; set; }
        public decimal CommitteeWeightPercent { get; set; }

        /// <summary>Điểm tổng kết tối thiểu để đạt.</summary>
        public decimal PassingScore { get; set; } = 5.0m;

        /// <summary>Mức tối thiểu của từng cột điểm bắt buộc; 0 nghĩa là tắt quy tắc này.</summary>
        public decimal MinComponentScore { get; set; } = 4.0m;

        public GradingMode GradingMode { get; set; } = GradingMode.CHAM_RIENG;

        public static ProjectTypeSettings CoSoDefaults() => new()
        {
            MaxMembers = 2,
            HasReviewer = false,
            AdvisorWeightPercent = 40,
            ReviewerWeightPercent = 0,
            CommitteeWeightPercent = 60
        };

        public static ProjectTypeSettings ChuyenNganhDefaults() => new()
        {
            MaxMembers = 3,
            HasReviewer = true,
            AdvisorWeightPercent = 20,
            ReviewerWeightPercent = 20,
            CommitteeWeightPercent = 60
        };

        /// <summary>QĐ 2006/QĐ-ĐHGTVT: 0,4 × quá trình (hướng dẫn, đọc duyệt) + 0,6 × bảo vệ.</summary>
        public static ProjectTypeSettings TotNghiepDefaults() => new()
        {
            MaxMembers = 1,
            HasReviewer = true,
            AdvisorWeightPercent = 20,
            ReviewerWeightPercent = 20,
            CommitteeWeightPercent = 60
        };

        public static ProjectTypeSettings MonHocDefaults() => new()
        {
            MaxMembers = 4,
            HasReviewer = false,
            AdvisorWeightPercent = 40,
            ReviewerWeightPercent = 0,
            CommitteeWeightPercent = 60
        };
    }

    /// <summary>Mục cấu hình "ProjectTypes". Thiếu mục nào thì dùng giá trị mặc định (đúng hành vi trước đây).</summary>
    public class ProjectTypesOptions
    {
        public const string SectionName = "ProjectTypes";

        [ConfigurationKeyName("DO_AN_CO_SO")]
        public ProjectTypeSettings CoSo { get; set; } = ProjectTypeSettings.CoSoDefaults();

        [ConfigurationKeyName("DO_AN_CHUYEN_NGANH")]
        public ProjectTypeSettings ChuyenNganh { get; set; } = ProjectTypeSettings.ChuyenNganhDefaults();

        [ConfigurationKeyName("DO_AN_MON_HOC")]
        public ProjectTypeSettings MonHoc { get; set; } = ProjectTypeSettings.MonHocDefaults();

        [ConfigurationKeyName("DO_AN_TOT_NGHIEP")]
        public ProjectTypeSettings TotNghiep { get; set; } = ProjectTypeSettings.TotNghiepDefaults();

        public ProjectTypeSettings Get(ProjectKind kind) => kind switch
        {
            ProjectKind.DO_AN_CO_SO => CoSo,
            ProjectKind.DO_AN_CHUYEN_NGANH => ChuyenNganh,
            ProjectKind.DO_AN_MON_HOC => MonHoc,
            ProjectKind.DO_AN_TOT_NGHIEP => TotNghiep,
            _ => throw new InvalidOperationException($"Chưa có cấu hình cho loại đồ án {kind}.")
        };
    }

    /// <summary>Kiểm tra cấu hình khi khởi động; sai thì ứng dụng dừng kèm danh sách lỗi.</summary>
    public class ProjectTypesOptionsValidator : IValidateOptions<ProjectTypesOptions>
    {
        public ValidateOptionsResult Validate(string? name, ProjectTypesOptions options)
        {
            var errors = new List<string>();
            foreach (var kind in Enum.GetValues<ProjectKind>())
                errors.AddRange(Check(kind, options.Get(kind)));
            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }

        public static IEnumerable<string> Check(ProjectKind kind, ProjectTypeSettings s)
        {
            var key = $"{ProjectTypesOptions.SectionName}:{kind}";
            if (s.MaxMembers < 1 || s.MaxMembers > 10)
                yield return $"{key}:MaxMembers = {s.MaxMembers} không hợp lệ (phải từ 1 đến 10).";
            if (s.AdvisorWeightPercent < 0 || s.ReviewerWeightPercent < 0 || s.CommitteeWeightPercent < 0)
                yield return $"{key}: trọng số không được âm.";
            var sum = s.AdvisorWeightPercent + s.ReviewerWeightPercent + s.CommitteeWeightPercent;
            if (sum != 100)
                yield return $"{key}: tổng trọng số hướng dẫn + phản biện + hội đồng là {sum}%, phải bằng 100%.";
            if (!s.HasReviewer && s.ReviewerWeightPercent != 0)
                yield return $"{key}: HasReviewer = false thì ReviewerWeightPercent phải bằng 0.";
            if (s.HasReviewer && s.ReviewerWeightPercent == 0)
                yield return $"{key}: HasReviewer = true thì ReviewerWeightPercent phải lớn hơn 0.";
            if (s.PassingScore <= 0 || s.PassingScore > 10)
                yield return $"{key}:PassingScore = {s.PassingScore} không hợp lệ (phải trong khoảng (0, 10]).";
            if (s.MinComponentScore < 0 || s.MinComponentScore > 10)
                yield return $"{key}:MinComponentScore = {s.MinComponentScore} không hợp lệ (0 để tắt, tối đa 10).";
            if (!Enum.IsDefined(s.GradingMode))
                yield return $"{key}:GradingMode không hợp lệ (CHAM_RIENG hoặc CHAM_CHUNG).";
        }
    }
}
