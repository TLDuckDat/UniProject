using Microsoft.EntityFrameworkCore;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;

namespace UniProject.API.Data
{
    /// <summary>
    /// Seed hai đợt đồ án môn học của CQ.65.CNTT theo SampleK65CourseProjects:
    /// Công nghệ Java (đã kết thúc, có tiến độ, bảo vệ, điểm, kết quả đã công bố) và Lập trình trực quan (đang thực hiện).
    /// Mỗi đợt chỉ seed một lần (theo mã đợt). Ngày tháng được ghi lùi về đúng thời điểm của học kỳ.
    /// </summary>
    public static class CourseProjectSeeder
    {
        private record Context(Lop Lop, ChuyenNganh Major, Dictionary<string, int> Students, Dictionary<string, GiangVien> Lecturers,
            SampleK65CourseProjects.CourseFile Data);

        public static async Task SeedAsync(AppDbContext db, ProjectTypePolicyResolver policies, IProjectGradeCalculator calculator,
            string contentRoot, ILogger logger)
        {
            var needJava = !await db.DotDoAns.AnyAsync(x => x.MaDotDoAnCode == SampleK65CourseProjects.Java.PeriodCode);
            var needVisual = !await db.DotDoAns.AnyAsync(x => x.MaDotDoAnCode == SampleK65CourseProjects.Visual.PeriodCode);
            if (!needJava && !needVisual) return;

            var data = await SeedFiles.ReadAsync<SampleK65CourseProjects.CourseFile>(contentRoot, SampleK65CourseProjects.FileName, logger);
            if (data == null) return;

            var ctx = await LoadContextAsync(db, data);
            if (ctx == null)
            {
                logger.LogWarning("Thiếu lớp {Class}, ngành hoặc sinh viên mẫu, bỏ qua seed đồ án môn học", SampleK65CourseProjects.ClassCode);
                return;
            }

            if (needJava) await SeedJavaAsync(db, ctx, policies.Get(ProjectKind.DO_AN_MON_HOC), calculator, logger);
            if (needVisual) await SeedVisualAsync(db, ctx, logger);
        }

        private static async Task<Context?> LoadContextAsync(AppDbContext db, SampleK65CourseProjects.CourseFile data)
        {
            var lop = await db.Lops.FirstOrDefaultAsync(x => x.MaLopCode == SampleK65CourseProjects.ClassCode);
            var major = await db.ChuyenNganhs.FirstOrDefaultAsync(x => x.MaChuyenNganhCode == SampleK65CourseProjects.MajorCode);
            if (lop == null || major == null) return null;

            var mssvs = data.Visual.Groups.SelectMany(g => g.Members).ToArray();
            var students = await db.SinhViens.Where(x => mssvs.Contains(x.MaSVCode)).ToDictionaryAsync(x => x.MaSVCode, x => x.Id);
            if (students.Count == 0) return null;

            var lecturers = (await db.GiangViens.ToListAsync())
                .ToDictionary(x => x.Email.Split('@')[0]);
            return new Context(lop, major, students, lecturers, data);
        }

        private static async Task<HocKy?> FindSemesterAsync(AppDbContext db, string schoolYear, int semester) =>
            await db.HocKys.FirstOrDefaultAsync(x => x.HocKySo == semester && x.NamHoc.TenNamHoc == schoolYear);

        /// <summary>Ghi lùi ngày tháng mà constructor / ChangeStatus mặc định lấy giờ hiện tại.</summary>
        private static void Stamp(AppDbContext db, object entity, string property, object? value) =>
            db.Entry(entity).Property(property).CurrentValue = value;

        private static DotDoAn CreatePeriod(string code, string name, int semesterId, int classId, PeriodStatus status,
            DateTime start, DateTime registration, DateTime outline, DateTime report, DateTime defenseStart, DateTime defenseEnd)
        {
            var period = new DotDoAn(code, name, ProjectKind.DO_AN_MON_HOC, semesterId, start, defenseEnd,
                registration, outline, report, defenseStart, defenseEnd);
            period.ChangeStatus(status);
            period.DotDoAnLops.Add(new DotDoAnLop(0, classId));
            return period;
        }

        // ---------------------------------------------------------------- Lập trình trực quan

        private static async Task SeedVisualAsync(AppDbContext db, Context ctx, ILogger logger)
        {
            var hocKy = await FindSemesterAsync(db, SampleK65CourseProjects.Visual.SchoolYear, SampleK65CourseProjects.Visual.Semester);
            if (hocKy == null || !ctx.Lecturers.TryGetValue(ctx.Data.Visual.Advisor, out var advisor))
            {
                logger.LogWarning("Thiếu học kỳ hoặc giảng viên, bỏ qua seed {Period}", SampleK65CourseProjects.Visual.PeriodCode);
                return;
            }

            var period = CreatePeriod(SampleK65CourseProjects.Visual.PeriodCode, SampleK65CourseProjects.Visual.PeriodName, hocKy.Id, ctx.Lop.Id,
                PeriodStatus.DANG_THUC_HIEN,
                SampleK65CourseProjects.Visual.StartDate, SampleK65CourseProjects.Visual.RegistrationDeadline, SampleK65CourseProjects.Visual.OutlineDeadline,
                SampleK65CourseProjects.Visual.ReportDeadline, SampleK65CourseProjects.Visual.DefenseStart, SampleK65CourseProjects.Visual.DefenseEnd);
            db.DotDoAns.Add(period);
            await db.SaveChangesAsync();

            var groups = ctx.Data.Visual.Groups
                .Select(g => (Data: g, Members: g.Members.Where(ctx.Students.ContainsKey).Select(m => ctx.Students[m]).ToArray()))
                .Where(g => g.Members.Length > 0)
                .ToArray();

            // Sinh viên tự đề xuất đề tài, GV dự kiến hướng dẫn duyệt; đăng ký trong tháng 9, duyệt đầu tháng 10
            var topics = groups.Select((g, i) =>
            {
                var t = new DeTai($"{SampleK65CourseProjects.Visual.CodePrefix}-{i + 1:00}", g.Data.Topic, g.Data.Content.Trim(),
                    SampleK65CourseProjects.Visual.Objective(g.Data.Topic), SampleK65CourseProjects.Visual.Requirements, g.Data.Stack,
                    4, advisor.Id, ctx.Major.Id, ProjectKind.DO_AN_MON_HOC, maSVDeXuat: g.Members[0]);
                t.Approve(advisor.Id);
                return t;
            }).ToArray();
            db.DeTais.AddRange(topics);
            for (var i = 0; i < topics.Length; i++)
            {
                Stamp(db, topics[i], nameof(DeTai.CreatedAt), new DateTime(2026, 9, 10).AddDays(i));
                Stamp(db, topics[i], nameof(DeTai.NgayDuyet), new DateTime(2026, 10, 1).AddDays(i % 3));
            }
            await db.SaveChangesAsync();

            var registrations = await CreateRegistrationsAsync(db, period, topics, groups.Select(g => g.Members).ToArray(), advisor.Id,
                i => new DateTime(2026, 9, 10).AddDays(i));

            var projects = groups.Select((g, i) =>
            {
                var p = new DoAn($"DA-{SampleK65CourseProjects.Visual.CodePrefix}-{i + 1:00}", g.Data.Topic, period.Id, topics[i].Id, registrations[i].Id);
                AddMembers(p, g.Members);
                p.AssignAdvisor(advisor.Id, AdvisorType.CHINH);
                p.ChangeStatus(ProjectStatus.DANG_THUC_HIEN);
                foreach (var f in g.Data.Feedbacks)
                {
                    var r = new YeuCauChinhSua(0, advisor.Id, f.Title, f.Content, SampleK65CourseProjects.Visual.OutlineDeadline);
                    r.ChangeStatus(f.Done ? RevisionStatus.DA_HOAN_THANH : RevisionStatus.CHUA_THUC_HIEN);
                    p.YeuCauChinhSuas.Add(r);
                }
                return p;
            }).ToArray();
            db.DoAns.AddRange(projects);
            foreach (var p in projects)
            {
                Stamp(db, p, nameof(DoAn.NgayTao), new DateTime(2026, 10, 3));
                Stamp(db, p, nameof(DoAn.NgayBatDau), new DateTime(2026, 10, 5));
                foreach (var r in p.YeuCauChinhSuas)
                    Stamp(db, r, nameof(YeuCauChinhSua.NgayYeuCau), new DateTime(2026, 10, 2));
            }
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded {Period}: {Groups} nhóm, {Students} sinh viên",
                SampleK65CourseProjects.Visual.PeriodCode, projects.Length, groups.Sum(g => g.Members.Length));
        }

        // ---------------------------------------------------------------- Công nghệ Java

        private static async Task SeedJavaAsync(AppDbContext db, Context ctx, IProjectTypePolicy policy, IProjectGradeCalculator calculator, ILogger logger)
        {
            var hocKy = await FindSemesterAsync(db, SampleK65CourseProjects.Java.SchoolYear, SampleK65CourseProjects.Java.Semester);
            var java = ctx.Data.Java;
            var committeeLecturers = java.Committee.Where(ctx.Lecturers.ContainsKey).Select(u => ctx.Lecturers[u]).ToArray();
            if (hocKy == null || !ctx.Lecturers.TryGetValue(java.Advisor, out var advisor)
                || committeeLecturers.Length != 3
                || !ctx.Lecturers.TryGetValue(java.HeadOfDepartment, out var headOfDepartment))
            {
                logger.LogWarning("Thiếu học kỳ hoặc giảng viên, bỏ qua seed {Period}", SampleK65CourseProjects.Java.PeriodCode);
                return;
            }

            var groups = BuildJavaGroups(ctx);
            var topicData = SampleK65CourseProjects.Java.Topics;

            var period = CreatePeriod(SampleK65CourseProjects.Java.PeriodCode, SampleK65CourseProjects.Java.PeriodName, hocKy.Id, ctx.Lop.Id,
                PeriodStatus.DA_KET_THUC,
                SampleK65CourseProjects.Java.StartDate, SampleK65CourseProjects.Java.RegistrationDeadline, SampleK65CourseProjects.Java.OutlineDeadline,
                SampleK65CourseProjects.Java.ReportDeadline, SampleK65CourseProjects.Java.DefenseStart, SampleK65CourseProjects.Java.DefenseEnd);
            db.DotDoAns.Add(period);
            await db.SaveChangesAsync();

            // Đề tài do GV giảng dạy ra, Trưởng Bộ môn duyệt trước khi mở đợt
            var topics = groups.Select((_, i) =>
            {
                var d = topicData[i % topicData.Length];
                var t = new DeTai($"{SampleK65CourseProjects.Java.CodePrefix}-{i + 1:00}", d.Topic, d.Content,
                    SampleK65CourseProjects.Java.Objective(d.Topic), SampleK65CourseProjects.Java.Requirements,
                    d.Network ? SampleK65CourseProjects.Java.NetworkStack : SampleK65CourseProjects.Java.Stack,
                    4, advisor.Id, ctx.Major.Id, ProjectKind.DO_AN_MON_HOC);
                t.Approve(headOfDepartment.Id);
                return t;
            }).ToArray();
            db.DeTais.AddRange(topics);
            foreach (var t in topics)
            {
                Stamp(db, t, nameof(DeTai.CreatedAt), new DateTime(2026, 1, 19));
                Stamp(db, t, nameof(DeTai.NgayDuyet), new DateTime(2026, 1, 23));
            }
            await db.SaveChangesAsync();

            var registrations = await CreateRegistrationsAsync(db, period, topics, groups, advisor.Id,
                i => new DateTime(2026, 1, 27).AddDays(i % 10), PeriodTopicStatus.DA_DONG);

            var rnd = new Random(2026);
            var projects = groups.Select((members, i) =>
            {
                var p = new DoAn($"DA-{SampleK65CourseProjects.Java.CodePrefix}-{i + 1:00}", topics[i].TenDeTai, period.Id, topics[i].Id, registrations[i].Id);
                AddMembers(p, members);
                p.AssignAdvisor(advisor.Id, AdvisorType.CHINH);
                foreach (var (r, n) in SampleK65CourseProjects.Java.ProgressReports.Select((r, n) => (r, n)))
                {
                    var report = new BaoCaoTienDo(0, members[0], n + 1, r.Title, $"{r.Title} của nhóm đề tài \"{topics[i].TenDeTai}\".",
                        r.Done, r.Next, n == 1 ? "Một số nghiệp vụ phức tạp cần trao đổi thêm với giảng viên." : null, r.Percent);
                    var evaluation = (ProgressEvaluation)Math.Clamp(2 + rnd.Next(-1, 3), 1, 4);
                    report.NhanXetTienDos.Add(new NhanXetTienDo(0, advisor.Id, ProgressComment(evaluation), evaluation));
                    p.BaoCaoTienDos.Add(report);
                }
                p.ChangeStatus(ProjectStatus.HOAN_THANH);
                return p;
            }).ToArray();
            db.DoAns.AddRange(projects);
            foreach (var p in projects)
            {
                Stamp(db, p, nameof(DoAn.NgayTao), new DateTime(2026, 2, 9));
                Stamp(db, p, nameof(DoAn.NgayBatDau), new DateTime(2026, 2, 9));
                Stamp(db, p, nameof(DoAn.NgayKetThuc), SampleK65CourseProjects.Java.DefenseEnd);
                foreach (var (report, n) in p.BaoCaoTienDos.Select((r, n) => (r, n)))
                {
                    var date = SampleK65CourseProjects.Java.ProgressReports[n].Date;
                    Stamp(db, report, nameof(BaoCaoTienDo.NgayNop), date);
                    Stamp(db, report, nameof(BaoCaoTienDo.CreatedAt), date);
                    foreach (var c in report.NhanXetTienDos)
                        Stamp(db, c, nameof(NhanXetTienDo.NgayNhanXet), date.AddDays(2));
                }
            }
            await db.SaveChangesAsync();

            // Hội đồng chấm và lịch bảo vệ: mỗi buổi sáng 8 nhóm, 30 phút / nhóm
            var committee = new HoiDong("HD-JAVA-K65", "Hội đồng chấm đồ án môn học Công nghệ Java - CQ.65.CNTT", period.Id, SampleK65CourseProjects.Java.Room);
            committee.AddMember(committeeLecturers[0].Id, CommitteeRole.CHU_TICH);
            committee.AddMember(committeeLecturers[1].Id, CommitteeRole.THU_KY);
            committee.AddMember(committeeLecturers[2].Id, CommitteeRole.UY_VIEN);
            committee.ChangeStatus(CommitteeStatus.DA_KET_THUC);
            db.HoiDongs.Add(committee);
            Stamp(db, committee, nameof(HoiDong.NgayTao), new DateTime(2026, 5, 25));
            await db.SaveChangesAsync();

            var schedules = projects.Select((p, i) =>
            {
                var start = TimeSpan.FromHours(7.5) + TimeSpan.FromMinutes(30 * (i % 8));
                var s = new LichBaoVe(p.Id, committee.Id, period.Id, SampleK65CourseProjects.Java.DefenseStart.AddDays(i / 8),
                    start, start + TimeSpan.FromMinutes(25), SampleK65CourseProjects.Java.Room);
                s.ChangeStatus(ScheduleStatus.HOAN_THANH);
                return s;
            }).ToArray();
            db.LichBaoVes.AddRange(schedules);
            await db.SaveChangesAsync();

            // Điểm: GV hướng dẫn chấm quá trình, 3 thành viên hội đồng chấm bảo vệ; điểm bước 0.5.
            // Một sinh viên ở nhóm cuối không đạt để có đủ trường hợp kết quả.
            var failing = groups[^1][^1];
            var fixedGroup = groups[0];
            var results = new List<KetQuaDoAn>();
            for (var i = 0; i < projects.Length; i++)
            {
                var groupBase = groups[i] == fixedGroup ? 8.5 : 6.0 + rnd.NextDouble() * 3.0;
                var defenseDate = SampleK65CourseProjects.Java.DefenseStart.AddDays(i / 8);
                foreach (var studentId in groups[i])
                {
                    var fails = studentId == failing;
                    var advisorScore = fails ? 4.0m : Score(groupBase + rnd.NextDouble() - 0.4);
                    var committeeScores = committeeLecturers
                        .Select(_ => fails ? Score(3.0 + rnd.NextDouble()) : Score(groupBase + rnd.NextDouble() * 1.5 - 0.75))
                        .ToArray();

                    var advisorSheet = new PhieuCham(projects[i].Id, studentId, advisor.Id, null, GradingType.HUONG_DAN, advisorScore, GradeComment(advisorScore));
                    db.PhieuChams.Add(advisorSheet);
                    Stamp(db, advisorSheet, nameof(PhieuCham.NgayCham), SampleK65CourseProjects.Java.ReportDeadline.AddDays(3));
                    for (var c = 0; c < committeeLecturers.Length; c++)
                    {
                        var sheet = new PhieuCham(projects[i].Id, studentId, committeeLecturers[c].Id, committee.Id, GradingType.HOI_DONG,
                            committeeScores[c], GradeComment(committeeScores[c]));
                        db.PhieuChams.Add(sheet);
                        Stamp(db, sheet, nameof(PhieuCham.NgayCham), defenseDate);
                    }

                    var committeeAvg = committeeScores.Average();
                    var outcome = calculator.Calculate(policy.Weights, policy.PassRules, advisorScore, null, committeeAvg)!;
                    var result = new KetQuaDoAn(projects[i].Id, studentId);
                    result.UpdateGrades(advisorScore, null, committeeAvg, outcome.FinalScore, outcome.Rank, outcome.Result);
                    result.Publish();
                    db.KetQuaDoAns.Add(result);
                    Stamp(db, result, nameof(KetQuaDoAn.NgayCongBo), SampleK65CourseProjects.Java.PublishDate);
                    results.Add(result);
                }
            }
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded {Period}: {Groups} nhóm, {Students} sinh viên, {Passed} đạt",
                SampleK65CourseProjects.Java.PeriodCode, projects.Length, results.Count, results.Count(r => r.KetQua == FinalResult.DAT));
        }

        /// <summary>
        /// Nhóm cố định đứng đầu; sinh viên còn lại (sinh viên K65 có trong danh sách LTTQ) xáo trộn cố định
        /// rồi chia nhóm 4, các nhóm cuối 3 người khi không chia hết.
        /// </summary>
        private static int[][] BuildJavaGroups(Context ctx)
        {
            var fixedIds = ctx.Data.FixedGroup.Where(ctx.Students.ContainsKey).Select(m => ctx.Students[m]).ToArray();
            var pool = ctx.Data.Visual.Groups.SelectMany(g => g.Members)
                .Where(m => m.StartsWith("65") && ctx.Students.ContainsKey(m) && !ctx.Data.FixedGroup.Contains(m))
                .Order()
                .Select(m => ctx.Students[m])
                .ToArray();
            new Random(65).Shuffle(pool);

            var groupCount = (pool.Length + 3) / 4;
            var threes = groupCount * 4 - pool.Length;
            var groups = new List<int[]> { fixedIds };
            var index = 0;
            for (var g = 0; g < groupCount; g++)
            {
                var size = g >= groupCount - threes ? 3 : 4;
                groups.Add(pool[index..(index + size)]);
                index += size;
            }
            return groups.Where(g => g.Length > 0).ToArray();
        }

        // ---------------------------------------------------------------- Dùng chung

        /// <summary>Mỗi đề tài mở trong đợt, mỗi nhóm một đơn đăng ký đã duyệt với đúng một nguyện vọng được chọn.</summary>
        private static async Task<DangKyDeTai[]> CreateRegistrationsAsync(AppDbContext db, DotDoAn period, DeTai[] topics, int[][] groups,
            int advisorId, Func<int, DateTime> registeredAt, PeriodTopicStatus periodTopicStatus = PeriodTopicStatus.DA_DONG)
        {
            var periodTopics = topics.Select(t => new DeTaiDotDoAn(t.Id, period.Id, periodTopicStatus)).ToArray();
            db.DeTaiDotDoAns.AddRange(periodTopics);
            await db.SaveChangesAsync();

            var registrations = groups.Select((members, i) =>
            {
                var r = new DangKyDeTai(period.Id, $"Nhóm {i + 1:00}");
                for (var m = 0; m < members.Length; m++)
                    r.AddMember(members[m], m == 0 ? GroupMemberRole.TRUONG_NHOM : GroupMemberRole.THANH_VIEN);
                r.AddPreference(periodTopics[i].Id, 1);
                r.Submit();
                r.Approve();
                r.ChiTietDangKys.Single().Approve(advisorId);
                return r;
            }).ToArray();
            db.DangKyDeTais.AddRange(registrations);
            for (var i = 0; i < registrations.Length; i++)
            {
                var at = registeredAt(i);
                Stamp(db, registrations[i], nameof(DangKyDeTai.NgayDangKy), at);
                Stamp(db, registrations[i], nameof(DangKyDeTai.CreatedAt), at);
                foreach (var member in registrations[i].ThanhViens)
                    Stamp(db, member, nameof(ThanhVienDangKy.NgayThamGia), at);
                Stamp(db, registrations[i].ChiTietDangKys.Single(), nameof(ChiTietDangKy.NgayDuyet), at.AddDays(3));
            }
            await db.SaveChangesAsync();
            return registrations;
        }

        private static void AddMembers(DoAn project, int[] members)
        {
            for (var m = 0; m < members.Length; m++)
                project.AddMember(members[m], m == 0 ? ProjectMemberRole.TRUONG_NHOM : ProjectMemberRole.THANH_VIEN, 4);
        }

        private static decimal Score(double value) => (decimal)Math.Clamp(Math.Round(value * 2) / 2, 0, 10);

        private static string GradeComment(decimal score) => score switch
        {
            >= 9 => "Sản phẩm hoàn thiện, giao diện tốt, trả lời vững các câu hỏi.",
            >= 8 => "Hoàn thành tốt các chức năng, nắm vững phần việc được giao.",
            >= 6.5m => "Đáp ứng yêu cầu, một số chức năng cần hoàn thiện thêm.",
            >= 5 => "Đạt yêu cầu tối thiểu, xử lý ngoại lệ và báo cáo còn sơ sài.",
            _ => "Chưa nắm được phần việc mình phụ trách, sản phẩm chưa đáp ứng yêu cầu."
        };

        private static string ProgressComment(ProgressEvaluation evaluation) => evaluation switch
        {
            ProgressEvaluation.RAT_TOT => "Nhóm làm việc rất tốt, vượt tiến độ đề ra.",
            ProgressEvaluation.TOT => "Tiến độ tốt, tiếp tục phát huy.",
            ProgressEvaluation.DAT => "Đạt tiến độ, cần chú ý chuẩn hóa cơ sở dữ liệu và đặt tên lớp theo quy ước.",
            _ => "Tiến độ chậm, nhóm cần phân công lại công việc và báo cáo bổ sung."
        };
    }
}
