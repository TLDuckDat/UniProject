using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniProject.API.Data;
using UniProject.API.Modules.Grading;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.ProjectPeriod
{
    /// <summary>
    /// Phần chung của các policy: tham số (quy mô nhóm, phản biện, trọng số, ngưỡng đạt, chế độ chấm)
    /// đọc từ cấu hình "ProjectTypes:&lt;LOAI_DO_AN&gt;"; giá trị mặc định nằm ở ProjectTypeSettings.
    /// </summary>
    public abstract class ConfiguredProjectTypePolicy : IProjectTypePolicy
    {
        protected ConfiguredProjectTypePolicy(ProjectKind kind, IOptions<ProjectTypesOptions> options)
        {
            Kind = kind;
            var s = options.Value.Get(kind);
            MaxMembers = s.MaxMembers;
            RequiresReviewer = s.HasReviewer;
            Weights = new GradeWeights(s.AdvisorWeightPercent / 100m, s.ReviewerWeightPercent / 100m, s.CommitteeWeightPercent / 100m);
            PassRules = new PassRules(s.PassingScore, s.MinComponentScore > 0 ? s.MinComponentScore : null);
            GradingMode = s.GradingMode;
        }

        public ProjectKind Kind { get; }
        public string DisplayName => Kind.ToDisplayName();
        public int MaxMembers { get; }
        public bool RequiresReviewer { get; }
        public GradeWeights Weights { get; }
        public PassRules PassRules { get; }
        public GradingMode GradingMode { get; }

        public abstract Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Đồ án cơ sở. Mặc định: nhóm tối đa 2 SV, không có phản biện, điểm = 40% hướng dẫn + 60% hội đồng.
    /// Không có điều kiện tiên quyết.
    /// </summary>
    public class DoAnCoSoPolicy : ConfiguredProjectTypePolicy
    {
        public DoAnCoSoPolicy(IOptions<ProjectTypesOptions> options) : base(ProjectKind.DO_AN_CO_SO, options) { }

        public override Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Đồ án chuyên ngành. Mặc định: nhóm tối đa 3 SV, có phản biện, điểm = 20% hướng dẫn + 20% phản biện + 60% hội đồng.
    /// Điều kiện tiên quyết: sinh viên đã ĐẠT một đồ án cơ sở (kết quả đã công bố).
    /// </summary>
    public class DoAnChuyenNganhPolicy : ConfiguredProjectTypePolicy
    {
        private readonly AppDbContext _dbContext;

        public DoAnChuyenNganhPolicy(AppDbContext dbContext, IOptions<ProjectTypesOptions> options)
            : base(ProjectKind.DO_AN_CHUYEN_NGANH, options)
        {
            _dbContext = dbContext;
        }

        public override async Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default)
        {
            var passedFoundation = await (
                from kq in _dbContext.KetQuaDoAns
                join da in _dbContext.DoAns on kq.MaDoAn equals da.Id
                join dot in _dbContext.DotDoAns on da.MaDotDoAn equals dot.Id
                where kq.MaSV == studentId
                      && kq.KetQua == FinalResult.DAT
                      && kq.NgayCongBo != null
                      && dot.LoaiDoAn == ProjectKind.DO_AN_CO_SO
                select kq.Id).AnyAsync(cancellationToken);

            if (!passedFoundation)
                throw new BusinessRuleException($"Sinh viên ID {studentId} chưa đạt Đồ án cơ sở nên chưa được tham gia Đồ án chuyên ngành.");
        }
    }

    /// <summary>
    /// Đồ án môn học (đồ án của một học phần). Mặc định: nhóm tối đa 4 SV, không có phản biện,
    /// điểm = 40% hướng dẫn + 60% hội đồng. Không có điều kiện tiên quyết.
    /// </summary>
    public class DoAnMonHocPolicy : ConfiguredProjectTypePolicy
    {
        public DoAnMonHocPolicy(IOptions<ProjectTypesOptions> options) : base(ProjectKind.DO_AN_MON_HOC, options) { }

        public override Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Đồ án tốt nghiệp. Mặc định: mỗi đề tài 1 SV, có giảng viên đọc duyệt (phản biện),
    /// điểm = 20% hướng dẫn + 20% đọc duyệt + 60% hội đồng. Điều kiện nhận ĐATN do Phòng Đào tạo xét, hệ thống không kiểm tra thêm.
    /// </summary>
    public class DoAnTotNghiepPolicy : ConfiguredProjectTypePolicy
    {
        public DoAnTotNghiepPolicy(IOptions<ProjectTypesOptions> options) : base(ProjectKind.DO_AN_TOT_NGHIEP, options) { }

        public override Task EnsureStudentEligibleAsync(int studentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Factory: trả về policy đúng với loại đồ án. Thêm loại đồ án mới chỉ cần viết policy mới
    /// và đăng ký trong ProjectPeriodModule, không phải sửa các service đang dùng.
    /// </summary>
    public class ProjectTypePolicyResolver
    {
        private readonly IReadOnlyDictionary<ProjectKind, IProjectTypePolicy> _policies;

        public ProjectTypePolicyResolver(IEnumerable<IProjectTypePolicy> policies)
        {
            _policies = policies.ToDictionary(p => p.Kind);
        }

        public IReadOnlyCollection<IProjectTypePolicy> All => _policies.Values.ToList();

        public IProjectTypePolicy Get(ProjectKind kind) =>
            _policies.TryGetValue(kind, out var policy)
                ? policy
                : throw new InvalidOperationException($"Chưa đăng ký policy cho loại đồ án {kind}.");

        /// <summary>Policy theo đợt đồ án (đọc loại đồ án của đợt từ DB).</summary>
        public async Task<IProjectTypePolicy> ForPeriodAsync(AppDbContext dbContext, int periodId, CancellationToken cancellationToken = default)
        {
            var kind = await dbContext.DotDoAns.Where(x => x.Id == periodId).Select(x => (ProjectKind?)x.LoaiDoAn).FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Đợt đồ án", periodId);
            return Get(kind);
        }

        /// <summary>Policy theo đồ án (qua đợt của đồ án).</summary>
        public async Task<IProjectTypePolicy> ForProjectAsync(AppDbContext dbContext, int projectId, CancellationToken cancellationToken = default)
        {
            var kind = await dbContext.DoAns.Where(x => x.Id == projectId)
                .Join(dbContext.DotDoAns, da => da.MaDotDoAn, dot => dot.Id, (da, dot) => (ProjectKind?)dot.LoaiDoAn)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Đồ án", projectId);
            return Get(kind);
        }

        public static ProjectKind Parse(string value) =>
            Enum.TryParse<ProjectKind>(value, true, out var kind) && Enum.IsDefined(kind)
                ? kind
                : throw new ValidationException("ProjectKind", $"Loại đồ án '{value}' không hợp lệ. Hợp lệ: {string.Join(", ", Enum.GetNames<ProjectKind>())}.");
    }
}
