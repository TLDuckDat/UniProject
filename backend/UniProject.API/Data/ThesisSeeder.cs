using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Identity;
using UniProject.API.Modules.Notification;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Data
{
    /// <summary>
    /// Seed đợt Đồ án tốt nghiệp giao tháng 03/2026 (K63 và sinh viên các khóa trước), theo:
    /// - dept.utc2.edu.vn/bomoncntt: kế hoạch thực hiện (17/03/2026), danh sách phân công GVHD (25/03/2026),
    ///   danh sách đủ điều kiện bảo vệ (01/07/2026), lễ bảo vệ 02 - 04/07/2026;
    /// - Data/Seed/datn-2026-03.json: đề tài và phiếu nhiệm vụ tách từ báo cáo ĐATN của sinh viên, phân công đọc duyệt và hội đồng
    ///   (sinh viên không có báo cáo dùng đề tài mẫu, "sample": true); file báo cáo PDF ở Data/Seed/datn-2026-03/.
    /// Hai file này chứa dữ liệu thật của sinh viên nên không commit lên Git; thiếu thì bỏ qua seed.
    /// Phân công đọc duyệt, hội đồng, lịch bảo vệ, tiến độ, điểm và nhận xét là dữ liệu mẫu.
    /// </summary>
    public static class ThesisSeeder
    {
        public const string PeriodCode = "DATN-03-2026";
        private const string PeriodName = "Đồ án tốt nghiệp - Đợt giao tháng 03/2026";
        private const string DataFile = "datn-2026-03.json";
        private const string ReportFolder = "datn-2026-03";
        private const string MajorCode = "7480201";

        private static readonly DateTime AssignStart = new(2026, 3, 20);
        private static readonly DateTime AssignEnd = new(2026, 3, 24);
        private static readonly DateTime AssignmentPublished = new(2026, 3, 26);
        private static readonly DateTime OutlineDeadline = new(2026, 3, 31);
        private static readonly DateTime TopicApproved = new(2026, 4, 2);
        private static readonly DateTime WorkStart = new(2026, 3, 23);
        private static readonly DateTime ReportStart = new(2026, 6, 22);
        private static readonly DateTime ReportDeadline = new(2026, 6, 24);
        private static readonly DateTime ReviewStart = new(2026, 6, 25);
        private static readonly DateTime EligibleListPublished = new(2026, 7, 1);
        private static readonly DateTime DefenseStart = new(2026, 7, 2);
        private static readonly DateTime DefenseEnd = new(2026, 7, 4);
        private static readonly DateTime PublishDate = new(2026, 7, 10);

        private const string CommonRequirements =
            "Yêu cầu chung (kế hoạch của Bộ môn CNTT): nộp đề cương, kế hoạch thực hiện cho GVHD chậm nhất 31/03/2026; " +
            "làm đồ án dưới sự hướng dẫn của GV từ 23/03 đến 20/06/2026, GVHD theo dõi và chấm điểm chuyên cần; " +
            "nộp báo cáo 22 - 24/06/2026 theo mẫu báo cáo ĐATN của Bộ môn, có chữ ký xác nhận đồng ý cho bảo vệ của GVHD; " +
            "làm việc trực tiếp với GV đọc duyệt 25 - 27/06/2026; bảo vệ trước hội đồng 02 - 04/07/2026.";

        private record StudentRow(
            string Mssv, string HoTen, string ClassCode, string Advisor, bool Eligible, bool Sample, string Title,
            string? Reason, string? Purpose, string? Objective, string? Scope, string? Method, string? Tech,
            string? Results, string? Plan, string[]? Chapters, string? Report);

        /// <summary>Hội đồng: Members theo thứ tự Chủ tịch, Thư ký, rồi các Ủy viên.</summary>
        private record CommitteeSeed(string Code, string Name, string Room, string[] Members);

        /// <summary>Reviewers: giảng viên đọc duyệt (không đọc duyệt đồ án mình hướng dẫn).</summary>
        private record DataFileRoot(string Source, StudentRow[] Students, string[] Reviewers, CommitteeSeed[] Committees);

        public static async Task SeedAsync(AppDbContext db, ProjectTypePolicyResolver policies, IProjectGradeCalculator calculator,
            string contentRoot, ILogger logger)
        {
            if (await db.DotDoAns.AnyAsync(x => x.MaDotDoAnCode == PeriodCode)) return;

            var dataPath = SeedFiles.Find(contentRoot, DataFile);
            if (dataPath == null)
            {
                logger.LogWarning("Không thấy Data/Seed/{File}, bỏ qua seed đồ án tốt nghiệp", DataFile);
                return;
            }
            var data = JsonSerializer.Deserialize<DataFileRoot>(await File.ReadAllTextAsync(dataPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var rows = data.Students;
            var reviewers = data.Reviewers;
            var committeeSeeds = data.Committees;
            var reportFolder = Path.Combine(SeedFiles.FolderOf(dataPath), ReportFolder);

            var lecturers = (await db.GiangViens.ToListAsync()).ToDictionary(x => x.Email.Split('@')[0]);
            var hocKy = await db.HocKys.FirstOrDefaultAsync(x => x.HocKySo == 2 && x.NamHoc.TenNamHoc == "2025-2026");
            var major = await db.ChuyenNganhs.FirstOrDefaultAsync(x => x.MaChuyenNganhCode == MajorCode);
            var svRole = await db.VaiTros.FirstOrDefaultAsync(x => x.TenVaiTro == AppRoles.SinhVien);
            var admin = await db.TaiKhoans.FirstOrDefaultAsync(x => x.TenDangNhap == "admin");
            var needed = rows.Select(r => r.Advisor).Concat(reviewers).Concat(committeeSeeds.SelectMany(c => c.Members)).Distinct();
            if (hocKy == null || major == null || svRole == null || needed.Any(u => !lecturers.ContainsKey(u)))
            {
                logger.LogWarning("Thiếu học kỳ, ngành, vai trò hoặc giảng viên, bỏ qua seed {Period}", PeriodCode);
                return;
            }

            var classes = await EnsureClassesAsync(db, rows, major.Id);
            var students = await EnsureStudentsAsync(db, rows, classes, svRole.Id, logger);

            // ---- Đợt đồ án
            var period = new DotDoAn(PeriodCode, PeriodName, ProjectKind.DO_AN_TOT_NGHIEP, hocKy.Id, AssignStart, DefenseEnd,
                AssignEnd, OutlineDeadline, ReportDeadline, DefenseStart, DefenseEnd);
            period.ChangeStatus(PeriodStatus.DA_KET_THUC);
            foreach (var classId in rows.Select(r => classes[r.ClassCode]).Distinct())
                period.DotDoAnLops.Add(new DotDoAnLop(0, classId));
            db.DotDoAns.Add(period);
            await db.SaveChangesAsync();

            // ---- Đề tài: sinh viên đề xuất kèm GVHD, GVHD duyệt sau khi nhận đề cương
            var topics = rows.Select((r, i) =>
            {
                var t = new DeTai($"DATN26-{i + 1:000}", Truncate(r.Title, 250), Description(r), Objective(r), Requirements(r),
                    r.Tech != null ? Truncate(r.Tech, 250) : null, 1, lecturers[r.Advisor].Id, major.Id, ProjectKind.DO_AN_TOT_NGHIEP,
                    maSVDeXuat: students[r.Mssv].Id);
                t.Approve(lecturers[r.Advisor].Id);
                return t;
            }).ToArray();
            db.DeTais.AddRange(topics);
            foreach (var t in topics)
            {
                Stamp(db, t, nameof(DeTai.CreatedAt), AssignEnd);
                Stamp(db, t, nameof(DeTai.NgayDuyet), TopicApproved);
            }
            await db.SaveChangesAsync();

            var periodTopics = topics.Select(t => new DeTaiDotDoAn(t.Id, period.Id, PeriodTopicStatus.DA_DONG)).ToArray();
            db.DeTaiDotDoAns.AddRange(periodTopics);
            await db.SaveChangesAsync();

            // ---- Đơn đăng ký (mỗi sinh viên một đơn, một nguyện vọng được chọn)
            var registrations = rows.Select((r, i) =>
            {
                var reg = new DangKyDeTai(period.Id, $"ĐATN - {r.HoTen}");
                reg.AddMember(students[r.Mssv].Id, GroupMemberRole.TRUONG_NHOM);
                reg.AddPreference(periodTopics[i].Id, 1);
                reg.Submit();
                reg.Approve();
                reg.ChiTietDangKys.Single().Approve(lecturers[r.Advisor].Id);
                return reg;
            }).ToArray();
            db.DangKyDeTais.AddRange(registrations);
            for (var i = 0; i < registrations.Length; i++)
            {
                var at = AssignStart.AddDays(i % 5);
                Stamp(db, registrations[i], nameof(DangKyDeTai.NgayDangKy), at);
                Stamp(db, registrations[i], nameof(DangKyDeTai.CreatedAt), at);
                Stamp(db, registrations[i].ThanhViens.Single(), nameof(ThanhVienDangKy.NgayThamGia), at);
                Stamp(db, registrations[i].ChiTietDangKys.Single(), nameof(ChiTietDangKy.NgayDuyet), AssignmentPublished);
            }
            await db.SaveChangesAsync();

            // ---- Đồ án, tiến độ, yêu cầu chỉnh sửa
            var rnd = new Random(2026);
            var projects = rows.Select((r, i) =>
            {
                var advisorId = lecturers[r.Advisor].Id;
                var p = new DoAn($"DA-DATN26-{i + 1:000}", topics[i].TenDeTai, period.Id, topics[i].Id, registrations[i].Id);
                p.AddMember(students[r.Mssv].Id, ProjectMemberRole.TRUONG_NHOM, 1);
                p.AssignAdvisor(advisorId, AdvisorType.CHINH);
                AddProgressReports(p, r, students[r.Mssv].Id, advisorId, rnd);
                if (r.Eligible)
                {
                    p.ChangeStatus(ProjectStatus.HOAN_THANH);
                }
                else
                {
                    p.ChangeStatus(ProjectStatus.TAM_DUNG);
                    var req = new YeuCauChinhSua(0, advisorId, "Chưa đủ điều kiện bảo vệ",
                        "Sinh viên chưa hoàn thành đồ án và không có tên trong danh sách đủ điều kiện bảo vệ đợt giao tháng 03/2026. " +
                        "Hoàn thiện sản phẩm, báo cáo và đăng ký thực hiện tiếp ở đợt sau.", null);
                    p.YeuCauChinhSuas.Add(req);
                }
                return p;
            }).ToArray();
            db.DoAns.AddRange(projects);
            foreach (var p in projects)
            {
                Stamp(db, p, nameof(DoAn.NgayTao), AssignmentPublished);
                Stamp(db, p, nameof(DoAn.NgayBatDau), WorkStart);
                Stamp(db, p, nameof(DoAn.NgayKetThuc), p.TrangThai == ProjectStatus.HOAN_THANH ? DefenseEnd : null);
                foreach (var (report, n) in p.BaoCaoTienDos.Select((r, n) => (r, n)))
                {
                    var date = ProgressSteps[n].Date;
                    Stamp(db, report, nameof(BaoCaoTienDo.NgayNop), date);
                    Stamp(db, report, nameof(BaoCaoTienDo.CreatedAt), date);
                    foreach (var c in report.NhanXetTienDos)
                        Stamp(db, c, nameof(NhanXetTienDo.NgayNhanXet), date.AddDays(2));
                }
                foreach (var req in p.YeuCauChinhSuas)
                    Stamp(db, req, nameof(YeuCauChinhSua.NgayYeuCau), EligibleListPublished);
            }
            await db.SaveChangesAsync();

            var eligible = rows.Select((r, i) => (Row: r, Project: projects[i], StudentId: students[r.Mssv].Id))
                .Where(x => x.Row.Eligible).ToArray();

            var documents = AttachReports(db, eligible.Select(e => (e.Row, e.Project, e.StudentId)), reportFolder, contentRoot, logger);

            // ---- Đọc duyệt: chia đều cho GV cơ hữu, khác GVHD
            var reviewLoad = reviewers.ToDictionary(u => u, _ => 0);
            var reviewerOf = new Dictionary<int, GiangVien>();
            foreach (var e in eligible)
            {
                var u = reviewLoad.Where(x => x.Key != e.Row.Advisor).OrderBy(x => x.Value).ThenBy(x => x.Key).First().Key;
                reviewLoad[u]++;
                reviewerOf[e.Project.Id] = lecturers[u];
                var review = new PhanBienDoAn(e.Project.Id, lecturers[u].Id);
                review.ChangeStatus(ReviewStatus.DA_PHAN_BIEN);
                db.PhanBienDoAns.Add(review);
                Stamp(db, review, nameof(PhanBienDoAn.NgayPhanCong), ReportDeadline);
                Stamp(db, review, nameof(PhanBienDoAn.CreatedAt), ReportDeadline);
            }
            await db.SaveChangesAsync();

            // ---- Hội đồng và lịch bảo vệ: sinh viên vào hội đồng không có GVHD của mình, chia đều
            var committees = committeeSeeds.Select(c =>
            {
                var h = new HoiDong(c.Code, c.Name, period.Id, c.Room);
                for (var m = 0; m < c.Members.Length; m++)
                    h.AddMember(lecturers[c.Members[m]].Id, m switch { 0 => CommitteeRole.CHU_TICH, 1 => CommitteeRole.THU_KY, _ => CommitteeRole.UY_VIEN });
                h.ChangeStatus(CommitteeStatus.DA_KET_THUC);
                return h;
            }).ToArray();
            db.HoiDongs.AddRange(committees);
            foreach (var h in committees) Stamp(db, h, nameof(HoiDong.NgayTao), new DateTime(2026, 6, 29));
            await db.SaveChangesAsync();

            var committeeOf = new Dictionary<int, int>();
            var load = new int[committees.Length];
            foreach (var e in eligible)
            {
                var candidates = Enumerable.Range(0, committees.Length)
                    .Where(c => !committeeSeeds[c].Members.Contains(e.Row.Advisor)).DefaultIfEmpty(0);
                var c = candidates.OrderBy(x => load[x]).First();
                committeeOf[e.Project.Id] = c;
                load[c]++;
            }
            var schedules = new List<LichBaoVe>();
            for (var c = 0; c < committees.Length; c++)
            {
                var list = eligible.Where(e => committeeOf[e.Project.Id] == c).ToArray();
                var perDay = (int)Math.Ceiling(list.Length / 3.0);
                for (var k = 0; k < list.Length; k++)
                {
                    var day = DefenseStart.AddDays(k / perDay);
                    var slot = k % perDay;
                    // Sáng 07:30 - 11:30, chiều từ 13:30; mỗi sinh viên 40 phút
                    var start = slot < 6 ? TimeSpan.FromHours(7.5) + TimeSpan.FromMinutes(40 * slot) : TimeSpan.FromHours(13.5) + TimeSpan.FromMinutes(40 * (slot - 6));
                    var s = new LichBaoVe(list[k].Project.Id, committees[c].Id, period.Id, day, start, start + TimeSpan.FromMinutes(35), committeeSeeds[c].Room);
                    s.ChangeStatus(ScheduleStatus.HOAN_THANH);
                    schedules.Add(s);
                }
            }
            db.LichBaoVes.AddRange(schedules);
            await db.SaveChangesAsync();

            // ---- Chấm điểm và kết quả
            var policy = policies.Get(ProjectKind.DO_AN_TOT_NGHIEP);
            var results = new List<KetQuaDoAn>();
            var revisions = 0;
            foreach (var e in eligible)
            {
                var advisor = lecturers[e.Row.Advisor];
                var reviewer = reviewerOf[e.Project.Id];
                var committee = committees[committeeOf[e.Project.Id]];
                var memberIds = committeeSeeds[committeeOf[e.Project.Id]].Members.Select(u => lecturers[u].Id).ToArray();
                var defenseDay = schedules.First(s => s.MaDoAn == e.Project.Id).NgayBaoVe;

                // Mức chung của đồ án; đồ án có báo cáo đầy đủ nhỉnh hơn một chút
                var level = 7.0 + rnd.NextDouble() * 2.2 + (e.Row.Report != null ? 0.2 : 0);
                var advisorScore = Score(level + rnd.NextDouble() * 0.8 - 0.2);
                var reviewerScore = Score(level + rnd.NextDouble() * 0.8 - 0.6);
                var committeeScores = memberIds.Select(_ => Score(level + rnd.NextDouble() * 1.0 - 0.5)).ToArray();

                AddSheet(db, e.Project.Id, e.StudentId, advisor.Id, null, GradingType.HUONG_DAN, advisorScore,
                    AdvisorComment(advisorScore), ReportDeadline);
                AddSheet(db, e.Project.Id, e.StudentId, reviewer.Id, null, GradingType.PHAN_BIEN, reviewerScore,
                    ReviewerComment(reviewerScore), ReviewStart.AddDays(2));
                for (var m = 0; m < memberIds.Length; m++)
                    AddSheet(db, e.Project.Id, e.StudentId, memberIds[m], committee.Id, GradingType.HOI_DONG, committeeScores[m],
                        CommitteeComment(committeeScores[m]), defenseDay);

                var committeeAvg = Math.Round(committeeScores.Average(), 2);
                var outcome = calculator.Calculate(policy.Weights, policy.PassRules, advisorScore, reviewerScore, committeeAvg)!;
                var result = new KetQuaDoAn(e.Project.Id, e.StudentId);
                result.UpdateGrades(advisorScore, reviewerScore, committeeAvg, outcome.FinalScore, outcome.Rank, outcome.Result);
                result.Publish();
                db.KetQuaDoAns.Add(result);
                Stamp(db, result, nameof(KetQuaDoAn.NgayCongBo), PublishDate);
                results.Add(result);

                // Hội đồng yêu cầu chỉnh sửa báo cáo trước khi nộp bản cuối (khoảng 40% đồ án)
                if (rnd.NextDouble() < 0.4)
                {
                    var req = new YeuCauChinhSua(e.Project.Id, memberIds[0], "Chỉnh sửa báo cáo theo góp ý hội đồng",
                        RevisionContent(rnd), defenseDay.AddDays(5));
                    req.ChangeStatus(RevisionStatus.DA_XAC_NHAN);
                    db.YeuCauChinhSuas.Add(req);
                    Stamp(db, req, nameof(YeuCauChinhSua.NgayYeuCau), defenseDay);
                    revisions++;
                }
            }
            await db.SaveChangesAsync();

            var notices = await AddNotificationsAsync(db, rows, reviewers, committeeSeeds, students, lecturers, admin?.Id, eligible.Length, results);

            logger.LogInformation(
                "Seeded {Period}: {Topics} đề tài, {Eligible} đủ điều kiện bảo vệ, {Docs} báo cáo PDF, {Passed} đạt, {Revisions} yêu cầu chỉnh sửa, {Notices} thông báo",
                PeriodCode, topics.Length, eligible.Length, documents, results.Count(r => r.KetQua == FinalResult.DAT), revisions, notices);
        }

        // ---------------------------------------------------------------- Lớp, sinh viên

        /// <summary>Tạo lớp còn thiếu (vd CQ.59.CNTT, CQ.60.CNTT); trả về mã lớp → id.</summary>
        private static async Task<Dictionary<string, int>> EnsureClassesAsync(AppDbContext db, StudentRow[] rows, int majorId)
        {
            var codes = rows.Select(r => r.ClassCode).Distinct().ToArray();
            var existing = await db.Lops.Where(x => codes.Contains(x.MaLopCode)).ToDictionaryAsync(x => x.MaLopCode, x => x.Id);
            foreach (var code in codes.Where(c => !existing.ContainsKey(c)))
            {
                var cohort = int.Parse(code.Split('.')[1]);
                var name = code.Contains(".CN.") ? $"Cử nhân Công nghệ thông tin K{cohort}" : $"Công nghệ thông tin K{cohort}";
                var lop = new Lop(code, name, SampleAcademicStructure.EnrollmentYear(cohort), majorId);
                db.Lops.Add(lop);
                await db.SaveChangesAsync();
                existing[code] = lop.Id;
            }
            return existing;
        }

        /// <summary>
        /// Sinh viên chưa có (khóa trước K62) được tạo tài khoản: tên đăng nhập và mật khẩu là MSSV.
        /// Sinh viên đã có được chuyển sang lớp quản lý ghi trong danh sách của Bộ môn (vd CQ.63.CN.CNTT).
        /// </summary>
        private static async Task<Dictionary<string, SinhVien>> EnsureStudentsAsync(AppDbContext db, StudentRow[] rows,
            Dictionary<string, int> classes, int svRoleId, ILogger logger)
        {
            var mssvs = rows.Select(r => r.Mssv).ToArray();
            var students = await db.SinhViens.Where(x => mssvs.Contains(x.MaSVCode)).ToDictionaryAsync(x => x.MaSVCode);

            foreach (var r in rows.Where(r => students.ContainsKey(r.Mssv) && students[r.Mssv].MaLop != classes[r.ClassCode]))
                await db.SinhViens.Where(x => x.MaSVCode == r.Mssv).ExecuteUpdateAsync(u => u.SetProperty(x => x.MaLop, classes[r.ClassCode]));

            var missing = rows.Where(r => !students.ContainsKey(r.Mssv)).ToArray();
            if (missing.Length > 0)
            {
                var accounts = missing.Select(r => new TaiKhoan(r.Mssv, BCrypt.Net.BCrypt.HashPassword(r.Mssv, 4),
                    $"{r.Mssv.ToLowerInvariant()}@st.utc2.edu.vn", r.HoTen, svRoleId)).ToArray();
                db.TaiKhoans.AddRange(accounts);
                await db.SaveChangesAsync();
                var created = missing.Select((r, i) => new SinhVien(r.Mssv, r.HoTen, null, null,
                    $"{r.Mssv.ToLowerInvariant()}@st.utc2.edu.vn", null, classes[r.ClassCode], accounts[i].Id)).ToArray();
                db.SinhViens.AddRange(created);
                await db.SaveChangesAsync();
                foreach (var s in created) students[s.MaSVCode] = s;
                logger.LogInformation("Tạo {Count} sinh viên khóa trước cho đợt ĐATN", created.Length);
            }
            return students;
        }

        // ---------------------------------------------------------------- Nội dung đề tài

        private static string Section(string heading, string? body) =>
            string.IsNullOrWhiteSpace(body) ? "" : $"{heading}:\n{body.Trim()}\n\n";

        private static string Description(StudentRow r)
        {
            var text = Section("Lý do chọn đề tài", r.Reason)
                + Section("Nội dung và phạm vi đề tài", r.Scope ?? r.Purpose)
                + Section("Phương pháp, công nghệ thực hiện", r.Method)
                + Section("Kế hoạch thực hiện", r.Plan)
                + (r.Chapters is { Length: > 0 } ? Section("Cấu trúc báo cáo", string.Join("\n", r.Chapters.Select(c => "- " + c))) : "");
            return string.IsNullOrWhiteSpace(text) ? r.Title : text.Trim();
        }

        private static string Objective(StudentRow r)
        {
            if (r.Sample)
                return $"Vận dụng kiến thức chuyên ngành để {char.ToLower(r.Title[0])}{r.Title[1..]}: khảo sát bài toán thực tế, " +
                       "phân tích và thiết kế hệ thống, cài đặt, kiểm thử và đánh giá kết quả; rèn luyện khả năng nghiên cứu độc lập.";
            return (Section("Mục đích, yêu cầu", r.Purpose) + Section("Mục tiêu", r.Objective)).Trim() is { Length: > 0 } s ? s : r.Title;
        }

        private static string Requirements(StudentRow r)
        {
            var results = r.Sample
                ? "- Sản phẩm phần mềm chạy được các chức năng trong phạm vi đề tài, có dữ liệu thực nghiệm.\n- Báo cáo đồ án tốt nghiệp theo mẫu của Bộ môn, slide bảo vệ."
                : r.Results;
            return Section("Các kết quả chính dự kiến", results) + CommonRequirements;
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 3)].TrimEnd() + "...";

        // ---------------------------------------------------------------- Tiến độ

        /// <summary>
        /// 4 lần báo cáo tiến độ (đủ điều kiện bảo vệ) hoặc 2 lần (chưa đủ điều kiện). Nội dung lấy theo kế hoạch / mục lục
        /// của chính báo cáo khi có, không thì theo các bước chung. Ngày nộp ghi trong SeedAsync sau khi entity được track.
        /// </summary>
        private static readonly (DateTime Date, int Percent, string Title, string Generic)[] ProgressSteps =
        [
            (new(2026, 4, 10), 20, "Báo cáo tiến độ lần 1 - Đề cương và cơ sở lý thuyết", "Hoàn thành đề cương, khảo sát tài liệu và cơ sở lý thuyết."),
            (new(2026, 5, 8), 45, "Báo cáo tiến độ lần 2 - Phân tích, thiết kế", "Phân tích yêu cầu, thiết kế hệ thống / mô hình, chuẩn bị dữ liệu."),
            (new(2026, 6, 5), 75, "Báo cáo tiến độ lần 3 - Cài đặt và thực nghiệm", "Cài đặt các chức năng chính, huấn luyện / thực nghiệm và đánh giá."),
            (new(2026, 6, 19), 100, "Báo cáo tiến độ lần 4 - Hoàn thiện sản phẩm và báo cáo", "Hoàn thiện sản phẩm, kiểm thử, viết báo cáo và slide bảo vệ."),
        ];

        private static void AddProgressReports(DoAn project, StudentRow r, int studentId, int advisorId, Random rnd)
        {
            var steps = ProgressSteps;
            var chapters = r.Chapters ?? [];
            var plan = (r.Plan ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart('-', ' ')).ToArray();
            var count = r.Eligible ? steps.Length : 2;
            for (var n = 0; n < count; n++)
            {
                var done = plan.Length >= 4
                    ? string.Join("\n", plan.Skip(n * plan.Length / 4).Take((n + 1) * plan.Length / 4 - n * plan.Length / 4))
                    : n < chapters.Length ? $"{steps[n].Generic}\nHoàn thành nội dung {chapters[n]}." : steps[n].Generic;
                var next = n + 1 < steps.Length ? steps[n + 1].Generic : "Nộp báo cáo cho GVHD ký xác nhận và chuẩn bị bảo vệ.";
                var percent = r.Eligible ? steps[n].Percent : steps[n].Percent - 10;
                var report = new BaoCaoTienDo(0, studentId, n + 1, steps[n].Title, $"{steps[n].Title}: {r.Title}.", done, next,
                    n == 2 ? "Thời gian huấn luyện / kiểm thử kéo dài, cần tối ưu thêm." : null, percent);

                var evaluation = r.Eligible
                    ? (ProgressEvaluation)Math.Clamp(2 + rnd.Next(0, 3), 2, 4)
                    : n == 0 ? ProgressEvaluation.CAN_CAI_THIEN : ProgressEvaluation.KHONG_DAT;
                report.NhanXetTienDos.Add(new NhanXetTienDo(0, advisorId, ProgressComment(evaluation), evaluation));
                project.BaoCaoTienDos.Add(report);
            }
        }

        // ---------------------------------------------------------------- Tài liệu

        /// <summary>Chép báo cáo PDF vào uploads/projects/{id} (như khi sinh viên tải lên) và ghi tài liệu loại BAO_CAO.</summary>
        private static int AttachReports(AppDbContext db, IEnumerable<(StudentRow Row, DoAn Project, int StudentId)> items, string reportFolder,
            string contentRoot, ILogger logger)
        {
            var count = 0;
            foreach (var (row, project, studentId) in items)
            {
                if (row.Report == null) continue;
                var source = Path.Combine(reportFolder, row.Report);
                if (!File.Exists(source))
                {
                    logger.LogWarning("Không thấy báo cáo {File}, bỏ qua tài liệu", source);
                    continue;
                }
                var relative = $"projects/{project.Id}/{Guid.NewGuid()}_{row.Mssv}_BaoCaoDATN.pdf";
                var target = Path.Combine(contentRoot, "uploads", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, overwrite: true);

                var doc = new TaiLieuDoAn(project.Id, studentId, $"Báo cáo đồ án tốt nghiệp - {row.HoTen}.pdf", relative, DocumentType.BAO_CAO, 1);
                db.TaiLieuDoAns.Add(doc);
                Stamp(db, doc, nameof(TaiLieuDoAn.NgayUpload), ReportStart.AddDays(count % 3));
                count++;
            }
            return count;
        }

        // ---------------------------------------------------------------- Thông báo

        private static async Task<int> AddNotificationsAsync(AppDbContext db, StudentRow[] rows, string[] reviewers, CommitteeSeed[] committees,
            Dictionary<string, SinhVien> students,
            Dictionary<string, GiangVien> lecturers, int? adminId, int eligibleCount, List<KetQuaDoAn> results)
        {
            var studentAccounts = rows.Select(r => students[r.Mssv].MaTaiKhoan).ToArray();
            var eligibleAccounts = rows.Where(r => r.Eligible).Select(r => students[r.Mssv].MaTaiKhoan).ToArray();
            var lecturerAccounts = rows.Select(r => r.Advisor).Concat(reviewers).Concat(committees.SelectMany(c => c.Members))
                .Distinct().Select(u => lecturers[u].MaTaiKhoan).ToArray();
            var passed = results.Count(r => r.KetQua == FinalResult.DAT);

            var notices = new (DateTime Date, string Title, string Content, NotificationPriority Priority, int[] To)[]
            {
                (new(2026, 3, 17, 14, 50, 0), "[Thông báo] Kế hoạch thực hiện học phần đồ án tốt nghiệp đợt giao tháng 03/2026",
                    "Kính gửi sinh viên CNTT ĐHCQ Khóa 63 và sinh viên các lớp đã kết thúc khóa học nhưng chưa hết thời gian đào tạo tối đa.\n" +
                    "1. Phân công hướng dẫn tốt nghiệp: 20/03 – 24/03/2026.\n2. Công bố danh sách phân công: 26/03/2026.\n" +
                    "3. Nộp đề cương, kế hoạch thực hiện: chậm nhất 31/03/2026 (GVHD tập hợp nộp Bộ môn chậm nhất 02/04/2026).\n" +
                    "4. Làm đồ án dưới sự hướng dẫn của GV: 23/03 – 20/06/2026.\n5. Nộp báo cáo: 22/06 – 24/06/2026 (GVHD ký xác nhận đồng ý cho bảo vệ).\n" +
                    "6. Đọc duyệt: 25/06 – 27/06/2026.\n7. Bảo vệ (dự kiến): 02/07 – 04/07/2026.",
                    NotificationPriority.QUAN_TRONG, studentAccounts.Concat(lecturerAccounts).ToArray()),
                (new(2026, 3, 25, 17, 7, 0), "[Thông báo] Danh sách phân công GVHD học phần đồ án tốt nghiệp đợt giao tháng 03/2026",
                    $"Bộ môn CNTT công bố danh sách {rows.Length} sinh viên nhận đồ án tốt nghiệp đợt 03/2026 và giảng viên hướng dẫn. " +
                    "Sinh viên liên hệ GVHD để nhận nhiệm vụ thiết kế tốt nghiệp và nộp đề cương chậm nhất 31/03/2026.",
                    NotificationPriority.QUAN_TRONG, studentAccounts.Concat(lecturerAccounts).ToArray()),
                (new(2026, 6, 25, 8, 0, 0), "Phân công đọc duyệt đồ án tốt nghiệp đợt giao tháng 03/2026",
                    "Sinh viên đã nộp báo cáo làm việc trực tiếp với giảng viên đọc duyệt từ 25/06 đến 27/06/2026. Xem giảng viên đọc duyệt trong trang đồ án.",
                    NotificationPriority.BINH_THUONG, eligibleAccounts.Concat(lecturerAccounts).ToArray()),
                (new(2026, 7, 1, 20, 12, 0), "Danh sách sinh viên đủ điều kiện bảo vệ đồ án tốt nghiệp (đợt giao tháng 3/2026)",
                    $"Có {eligibleCount} sinh viên đủ điều kiện bảo vệ. Lịch bảo vệ 02 – 04/07/2026 theo hội đồng, xem chi tiết trong mục Lịch bảo vệ.",
                    NotificationPriority.KHAN_CAP, studentAccounts.Concat(lecturerAccounts).ToArray()),
                (new(2026, 7, 4, 21, 44, 0), "Lễ bảo vệ đồ án tốt nghiệp ngành Công nghệ thông tin UTC2 đợt 2 năm học 2025 - 2026",
                    "Trong ba ngày 02, 03 và 04/7/2026, Bộ môn CNTT đã tổ chức thành công đợt bảo vệ đồ án tốt nghiệp. Nhiều đề tài bám sát xu hướng " +
                    "trí tuệ nhân tạo, thị giác máy tính, IoT, phát triển phần mềm, an toàn thông tin, dữ liệu lớn và chuyển đổi số. " +
                    "Sinh viên chỉnh sửa báo cáo theo góp ý của hội đồng (nếu có) trước khi nộp bản cuối.",
                    NotificationPriority.BINH_THUONG, eligibleAccounts.Concat(lecturerAccounts).ToArray()),
                (new(2026, 7, 10, 9, 0, 0), "Công bố kết quả đồ án tốt nghiệp đợt giao tháng 03/2026",
                    $"Kết quả đồ án tốt nghiệp đã được công bố: {passed}/{results.Count} sinh viên đạt. Sinh viên xem điểm chi tiết trong mục Kết quả.",
                    NotificationPriority.QUAN_TRONG, eligibleAccounts),
            };

            var rnd = new Random(63);
            foreach (var n in notices)
            {
                var notice = new ThongBao(n.Title, n.Content, "DO_AN_TOT_NGHIEP", n.Priority, adminId);
                foreach (var to in n.To.Distinct()) notice.AddRecipient(to);
                db.ThongBaos.Add(notice);
                Stamp(db, notice, nameof(ThongBao.NgayTao), n.Date);
                Stamp(db, notice, nameof(ThongBao.CreatedAt), n.Date);
                foreach (var recipient in notice.NguoiNhans.Where(_ => rnd.NextDouble() < 0.85))
                {
                    recipient.MarkAsRead();
                    Stamp(db, recipient, nameof(NguoiNhanThongBao.NgayDoc), n.Date.AddHours(rnd.Next(1, 72)));
                }
            }
            await db.SaveChangesAsync();
            return notices.Length;
        }

        // ---------------------------------------------------------------- Chấm điểm

        private static void AddSheet(AppDbContext db, int projectId, int studentId, int lecturerId, int? committeeId,
            GradingType type, decimal score, string comment, DateTime gradedAt)
        {
            var sheet = new PhieuCham(projectId, studentId, lecturerId, committeeId, type, score, comment);
            db.PhieuChams.Add(sheet);
            Stamp(db, sheet, nameof(PhieuCham.NgayCham), gradedAt);
            Stamp(db, sheet, nameof(PhieuCham.CreatedAt), gradedAt);
        }

        /// <summary>Điểm ĐATN chấm theo bước 0,25.</summary>
        private static decimal Score(double value) => (decimal)Math.Clamp(Math.Round(value * 4) / 4, 0, 10);

        private static string AdvisorComment(decimal s) => s switch
        {
            >= 9 => "Sinh viên chủ động, chuyên cần, báo cáo tiến độ đầy đủ; sản phẩm hoàn chỉnh, có tính ứng dụng cao. Đồng ý cho bảo vệ.",
            >= 8 => "Sinh viên làm việc nghiêm túc, đúng tiến độ; hoàn thành tốt các mục tiêu đề ra. Đồng ý cho bảo vệ.",
            >= 7 => "Hoàn thành các nội dung chính của nhiệm vụ thiết kế; một số phần thực nghiệm cần đánh giá sâu hơn. Đồng ý cho bảo vệ.",
            _ => "Hoàn thành ở mức yêu cầu, tiến độ có giai đoạn chậm; báo cáo cần chỉnh sửa trình bày. Đồng ý cho bảo vệ."
        };

        private static string ReviewerComment(decimal s) => s switch
        {
            >= 9 => "Báo cáo trình bày mạch lạc, đúng mẫu; cơ sở lý thuyết vững, kết quả thực nghiệm thuyết phục.",
            >= 8 => "Báo cáo đầy đủ, bố cục hợp lý; nên bổ sung so sánh với các nghiên cứu / sản phẩm liên quan.",
            >= 7 => "Đáp ứng yêu cầu đồ án tốt nghiệp; phần đánh giá kết quả còn sơ sài, cần bổ sung số liệu.",
            _ => "Báo cáo còn lỗi trình bày và trích dẫn; cần làm rõ đóng góp của đề tài."
        };

        private static string CommitteeComment(decimal s) => s switch
        {
            >= 9 => "Trình bày tự tin, demo ổn định, trả lời tốt các câu hỏi của hội đồng.",
            >= 8 => "Trình bày rõ ràng, demo đầy đủ chức năng; trả lời được phần lớn câu hỏi.",
            >= 7 => "Trình bày đạt yêu cầu; một số câu hỏi về cơ sở lý thuyết trả lời chưa trọn vẹn.",
            _ => "Trình bày còn lúng túng, demo chưa ổn định; cần nắm vững hơn phần việc đã làm."
        };

        private static string ProgressComment(ProgressEvaluation e) => e switch
        {
            ProgressEvaluation.RAT_TOT => "Tiến độ rất tốt, vượt kế hoạch. Tiếp tục hoàn thiện theo đề cương.",
            ProgressEvaluation.TOT => "Tiến độ tốt, nội dung đúng hướng.",
            ProgressEvaluation.DAT => "Đạt tiến độ; cần bổ sung tài liệu tham khảo và mô tả rõ phương pháp đánh giá.",
            ProgressEvaluation.CAN_CAI_THIEN => "Tiến độ chậm so với kế hoạch, cần tăng thời gian thực hiện và báo cáo thường xuyên hơn.",
            _ => "Chưa đạt tiến độ yêu cầu, nguy cơ không đủ điều kiện bảo vệ."
        };

        private static readonly string[] RevisionItems =
        [
            "Bổ sung bảng so sánh kết quả với các phương pháp / sản phẩm liên quan.",
            "Chuẩn hóa trích dẫn và danh mục tài liệu tham khảo theo mẫu.",
            "Làm rõ đóng góp mới của đề tài trong phần Tổng quan và Kết luận.",
            "Bổ sung sơ đồ kiến trúc tổng thể và mô tả luồng xử lý chính.",
            "Sửa lỗi chính tả, đánh số hình và bảng theo chương.",
            "Bổ sung hướng phát triển và hạn chế của đề tài.",
        ];

        private static string RevisionContent(Random rnd) =>
            string.Join("\n", RevisionItems.OrderBy(_ => rnd.Next()).Take(2).Select(x => "- " + x));

        private static void Stamp(AppDbContext db, object entity, string property, object? value) =>
            db.Entry(entity).Property(property).CurrentValue = value;
    }
}
