using Microsoft.EntityFrameworkCore;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Identity;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Data
{
    public static class DbInitializer
    {
        private const string LecturerDefaultPassword = "Lecturer@123456";

        // Data/Seed/students.json: { "CQ.64.CNTT": [["MSSV", "Họ tên"], ...] }
        private const string StudentsFileName = "students.json";

        // Data/Seed/lecturers.json. Học vị: 2 = Kỹ sư, 3 = Thạc sĩ, 4 = Tiến sĩ; học hàm: 1 = Phó Giáo sư.
        // Giảng viên cơ hữu được mã GV001, GV002... theo thứ tự trong file (GV001 là Trưởng Bộ môn), thỉnh giảng TG001...
        private const string LecturersFileName = "lecturers.json";
        private record LecturerSeed(string Username, string FullName, int DegreeId, int? TitleId, bool Visiting, string? Note);
        private record LecturerFile(LecturerSeed[] Lecturers);

        // Work factor thấp để seed vài nghìn tài khoản nhanh; lần đăng nhập đầu IdentityService băm lại với mức chuẩn
        private const int SeedWorkFactor = 4;

        /// <summary>
        /// Chạy migration và đảm bảo đủ 3 vai trò. Dữ liệu mẫu chỉ tạo khi bật SeedSampleData
        /// và database chưa có khoa nào, không ghi đè dữ liệu đã có.
        /// </summary>
        public static async Task InitializeAsync(AppDbContext context, IPasswordHasher passwordHasher, ProjectTypePolicyResolver policies,
            IProjectGradeCalculator gradeCalculator, string contentRoot, ILogger logger, bool seedSampleData)
        {
            await context.Database.MigrateAsync();

            var adminRole = await EnsureRoleAsync(context, 1, AppRoles.Admin, "Quản trị hệ thống");
            var gvRole = await EnsureRoleAsync(context, 2, AppRoles.GiangVien, "Giảng viên");
            var svRole = await EnsureRoleAsync(context, 3, AppRoles.SinhVien, "Sinh viên");

            if (!seedSampleData) return;

            if (!await context.TaiKhoans.AnyAsync(x => x.TenDangNhap == "admin"))
            {
                context.TaiKhoans.Add(new TaiKhoan("admin", passwordHasher.HashPassword("Admin@123456"), "admin@utc2.edu.vn", "Quản Trị Viên Hệ Thống", adminRole.Id));
                await context.SaveChangesAsync();
            }

            if (!await context.Khoas.AnyAsync())
            {
                var (lecturerCount, studentCount) = await SeedAcademicDataAsync(context, passwordHasher, contentRoot, logger, gvRole.Id, svRole.Id);
                logger.LogInformation("Seeded sample academic data: {Lecturers} giảng viên, {Students} sinh viên", lecturerCount, studentCount);
            }

            // Tách khỏi bước seed cơ cấu để bổ sung được vào database đã seed trước đó
            if (!await context.DotDoAns.AnyAsync(x => x.MaDotDoAnCode == SampleK65CnttProjects.PeriodCode))
                await SeedK65CnttProjectsAsync(context, contentRoot, logger);
            else
                await FillK65CnttTopicDetailsAsync(context, logger);

            await CourseProjectSeeder.SeedAsync(context, policies, gradeCalculator, contentRoot, logger);
            await ThesisSeeder.SeedAsync(context, policies, gradeCalculator, contentRoot, logger);
        }

        /// <summary>
        /// Đề tài mẫu K65 seed từ bản cũ chưa có mục tiêu, yêu cầu, công nghệ: bổ sung theo SampleK65CnttProjects.
        /// Chỉ đụng tới đề tài còn trống MucTieu nên không ghi đè nội dung đã sửa tay.
        /// </summary>
        private static async Task FillK65CnttTopicDetailsAsync(AppDbContext context, ILogger logger)
        {
            var pending = await context.DeTais.AsNoTracking()
                .Where(x => x.MaDeTaiCode.StartsWith("DT-K65-") && x.MucTieu == null)
                .Select(x => new { x.Id, x.TenDeTai, x.MaGVDeXuat })
                .ToListAsync();
            if (pending.Count == 0) return;

            var headOfDepartment = await context.GiangViens.AsNoTracking().Where(x => x.MaGVCode == "GV001").Select(x => (int?)x.Id).FirstOrDefaultAsync();
            foreach (var t in pending)
            {
                if (!SampleK65CnttProjects.Topics.TryGetValue(t.TenDeTai, out var detail)) continue;
                int? approver = t.MaGVDeXuat == headOfDepartment ? null : headOfDepartment;
                await context.DeTais.Where(x => x.Id == t.Id).ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MoTa, detail.Scope)
                    .SetProperty(x => x.MucTieu, SampleK65CnttProjects.Objective(detail.Purpose))
                    .SetProperty(x => x.YeuCau, SampleK65CnttProjects.Requirements)
                    .SetProperty(x => x.CongNgheDuKien, SampleK65CnttProjects.Stacks[detail.Stack])
                    .SetProperty(x => x.MaGVDuyet, approver));
            }
            logger.LogInformation("Bổ sung nội dung cho {Count} đề tài mẫu K65", pending.Count);
        }

        /// <summary>
        /// Đợt Đồ án cơ sở lớp CQ.65.CNTT đang thực hiện: đề tài đã duyệt, đơn đăng ký nhóm đã duyệt
        /// và đồ án đã phân công giảng viên hướng dẫn, theo SampleK65CnttProjects và Data/Seed/k65-do-an-co-so.json.
        /// </summary>
        private static async Task SeedK65CnttProjectsAsync(AppDbContext context, string contentRoot, ILogger logger)
        {
            var file = await SeedFiles.ReadAsync<SampleK65CnttProjects.AssignmentFile>(contentRoot, SampleK65CnttProjects.FileName, logger);
            if (file == null) return;
            var assignments = file.Assignments;

            var lop = await context.Lops.FirstOrDefaultAsync(x => x.MaLopCode == SampleK65CnttProjects.ClassCode);
            var major = await context.ChuyenNganhs.FirstOrDefaultAsync(x => x.MaChuyenNganhCode == SampleK65CnttProjects.MajorCode);
            var hocKy = await context.HocKys.FirstOrDefaultAsync(x =>
                x.HocKySo == SampleK65CnttProjects.Semester && x.NamHoc.TenNamHoc == SampleK65CnttProjects.SchoolYear);
            var lecturers = await context.GiangViens.Where(x => x.MaGVCode.StartsWith("GV")).OrderBy(x => x.MaGVCode).ToListAsync();
            if (lop == null || major == null || hocKy == null || lecturers.Count == 0)
            {
                logger.LogWarning("Thiếu lớp, ngành, học kỳ hoặc giảng viên mẫu, bỏ qua seed đồ án {Class}", SampleK65CnttProjects.ClassCode);
                return;
            }

            var mssvs = assignments.Select(a => a.Mssv).ToArray();
            var students = await context.SinhViens.Where(x => mssvs.Contains(x.MaSVCode)).ToDictionaryAsync(x => x.MaSVCode, x => x.Id);
            if (students.Count == 0)
            {
                logger.LogWarning("Chưa có sinh viên {Class}, bỏ qua seed đồ án", SampleK65CnttProjects.ClassCode);
                return;
            }
            foreach (var missing in mssvs.Where(m => !students.ContainsKey(m)))
                logger.LogWarning("Không thấy sinh viên {Mssv}, bỏ qua trong seed đồ án", missing);

            var period = new DotDoAn(SampleK65CnttProjects.PeriodCode, SampleK65CnttProjects.PeriodName, ProjectKind.DO_AN_CO_SO, hocKy.Id,
                SampleK65CnttProjects.StartDate, SampleK65CnttProjects.DefenseEnd,
                SampleK65CnttProjects.RegistrationDeadline, SampleK65CnttProjects.OutlineDeadline, SampleK65CnttProjects.ReportDeadline,
                SampleK65CnttProjects.DefenseStart, SampleK65CnttProjects.DefenseEnd);
            period.ChangeStatus(PeriodStatus.DANG_THUC_HIEN);
            period.DotDoAnLops.Add(new DotDoAnLop(0, lop.Id));
            context.DotDoAns.Add(period);
            await context.SaveChangesAsync();

            // Nhóm theo đề tài, giữ thứ tự xuất hiện trong danh sách
            var groups = assignments
                .Where(a => students.ContainsKey(a.Mssv))
                .GroupBy(a => a.Topic)
                .Select((g, i) => (No: i + 1, Topic: g.Key, Members: g.Select(a => students[a.Mssv]).ToArray(), Advisor: lecturers[i % lecturers.Count]))
                .ToArray();

            // Trưởng Bộ môn (GV001) duyệt đề tài; đề tài của chính Trưởng Bộ môn do Admin duyệt
            var headOfDepartment = lecturers[0];
            var topics = groups.Select(g =>
            {
                var detail = SampleK65CnttProjects.Topics.GetValueOrDefault(g.Topic)
                    ?? new(g.Topic.ToLowerInvariant(), $"Phân tích, thiết kế và xây dựng ứng dụng: {g.Topic}.", "aspnet");
                var topic = new DeTai($"DT-K65-{g.No:000}", g.Topic, detail.Scope,
                    SampleK65CnttProjects.Objective(detail.Purpose), SampleK65CnttProjects.Requirements, SampleK65CnttProjects.Stacks[detail.Stack],
                    2, g.Advisor.Id, major.Id, ProjectKind.DO_AN_CO_SO);
                topic.Approve(g.Advisor.Id == headOfDepartment.Id ? null : headOfDepartment.Id);
                return topic;
            }).ToArray();
            context.DeTais.AddRange(topics);
            await context.SaveChangesAsync();

            var periodTopics = topics.Select(t => new DeTaiDotDoAn(t.Id, period.Id, PeriodTopicStatus.DA_DONG)).ToArray();
            context.DeTaiDotDoAns.AddRange(periodTopics);
            await context.SaveChangesAsync();

            var registrations = groups.Select((g, i) =>
            {
                var registration = new DangKyDeTai(period.Id, $"Nhóm {g.No:00}");
                for (var m = 0; m < g.Members.Length; m++)
                    registration.AddMember(g.Members[m], m == 0 ? GroupMemberRole.TRUONG_NHOM : GroupMemberRole.THANH_VIEN);
                registration.AddPreference(periodTopics[i].Id, 1);
                registration.Submit();
                registration.Approve();
                registration.ChiTietDangKys.Single().Approve(g.Advisor.Id);
                return registration;
            }).ToArray();
            context.DangKyDeTais.AddRange(registrations);
            await context.SaveChangesAsync();

            var projects = groups.Select((g, i) =>
            {
                var project = new DoAn($"DA-K65-{g.No:000}", g.Topic, period.Id, topics[i].Id, registrations[i].Id);
                for (var m = 0; m < g.Members.Length; m++)
                    project.AddMember(g.Members[m], m == 0 ? ProjectMemberRole.TRUONG_NHOM : ProjectMemberRole.THANH_VIEN, topics[i].SoLuongSinhVienToiDa);
                project.AssignAdvisor(g.Advisor.Id, AdvisorType.CHINH);
                project.ChangeStatus(ProjectStatus.DANG_THUC_HIEN);
                return project;
            }).ToArray();
            context.DoAns.AddRange(projects);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Period}: {Topics} đề tài / đồ án, {Students} sinh viên",
                SampleK65CnttProjects.PeriodCode, topics.Length, groups.Sum(g => g.Members.Length));
        }

        private static async Task<VaiTro> EnsureRoleAsync(AppDbContext context, int id, string name, string description)
        {
            var role = await context.VaiTros.FirstOrDefaultAsync(x => x.TenVaiTro == name);
            if (role == null)
            {
                role = new VaiTro(id, name, description);
                context.VaiTros.Add(role);
                await context.SaveChangesAsync();
            }
            return role;
        }

        private static async Task<(int Lecturers, int Students)> SeedAcademicDataAsync(AppDbContext context, IPasswordHasher passwordHasher,
            string contentRoot, ILogger logger, int gvRoleId, int svRoleId)
        {
            if (!await context.HocVis.AnyAsync())
            {
                context.HocVis.AddRange(
                    new HocVi(1, "Cử nhân"),
                    new HocVi(2, "Kỹ sư"),
                    new HocVi(3, "Thạc sĩ"),
                    new HocVi(4, "Tiến sĩ"));
            }

            if (!await context.HocHams.AnyAsync())
            {
                context.HocHams.AddRange(
                    new HocHam(1, "Phó Giáo sư"),
                    new HocHam(2, "Giáo sư"));
            }
            await context.SaveChangesAsync();

            // Khoa -> Bộ môn -> Ngành / chuyên ngành, xem SampleAcademicStructure
            var departments = new Dictionary<string, BoMon>();
            var majors = new Dictionary<string, ChuyenNganh>();
            foreach (var f in SampleAcademicStructure.Faculties)
            {
                var khoa = new Khoa(f.Code, f.Name);
                context.Khoas.Add(khoa);
                await context.SaveChangesAsync();

                foreach (var d in f.Departments)
                {
                    var department = new BoMon(d.Code, d.Name, khoa.Id);
                    context.BoMons.Add(department);
                    await context.SaveChangesAsync();
                    departments[d.Code] = department;

                    foreach (var m in d.Majors)
                    {
                        majors[m.Code] = new ChuyenNganh(m.Code, m.Name, department.Id);
                        context.ChuyenNganhs.Add(majors[m.Code]);
                        context.ChuyenNganhs.AddRange(m.Specializations.Select(s => new ChuyenNganh(s.Code, s.Name, department.Id)));
                    }
                }
            }
            await context.SaveChangesAsync();

            var classes = SampleAcademicStructure.Classes.ToDictionary(
                c => c.Code,
                c => new Lop(c.Code, c.Name, SampleAcademicStructure.EnrollmentYear(c.Cohort), majors[c.MajorCode].Id));
            context.Lops.AddRange(classes.Values);
            await context.SaveChangesAsync();

            for (var year = 2024; year <= 2026; year++)
            {
                var namHoc = new NamHoc($"{year}-{year + 1}", new DateTime(year, 9, 1), new DateTime(year + 1, 6, 30));
                context.NamHocs.Add(namHoc);
                await context.SaveChangesAsync();

                context.HocKys.AddRange(
                    new HocKy("Học kỳ 1", 1, namHoc.Id, new DateTime(year, 9, 1), new DateTime(year + 1, 1, 15)),
                    new HocKy("Học kỳ 2", 2, namHoc.Id, new DateTime(year + 1, 1, 16), new DateTime(year + 1, 6, 30)));
            }
            await context.SaveChangesAsync();

            var boMon = departments[SampleAcademicStructure.CnttDepartmentCode];
            var boMonThinhGiang = departments[SampleAcademicStructure.VisitingDepartmentCode];

            // Giảng viên: tên đăng nhập theo quy ước email UTC2 (username trong lecturers.json, email <username>@utc2.edu.vn)
            var lecturerSeeds = (await SeedFiles.ReadAsync<LecturerFile>(contentRoot, LecturersFileName, logger))?.Lecturers ?? [];
            var lecturers = lecturerSeeds.Where(l => !l.Visiting)
                .Select((l, i) => (l.Username, HoTen: l.FullName, MaHocVi: l.DegreeId, MaHocHam: l.TitleId, Code: $"GV{i + 1:000}", MaBoMon: boMon.Id))
                .Concat(lecturerSeeds.Where(l => l.Visiting)
                    .Select((l, i) => (l.Username, HoTen: l.FullName, MaHocVi: l.DegreeId, MaHocHam: l.TitleId, Code: $"TG{i + 1:000}", MaBoMon: boMonThinhGiang.Id)))
                .ToArray();

            var lecturerHash = passwordHasher.HashPassword(LecturerDefaultPassword);
            var lecturerAccounts = lecturers
                .Select(l => new TaiKhoan(l.Username, lecturerHash, $"{l.Username}@utc2.edu.vn", l.HoTen, gvRoleId))
                .ToList();
            context.TaiKhoans.AddRange(lecturerAccounts);
            await context.SaveChangesAsync();

            context.GiangViens.AddRange(lecturers.Select((l, i) =>
                new GiangVien(l.Code, l.HoTen, $"{l.Username}@utc2.edu.vn", null, l.MaBoMon, l.MaHocVi, l.MaHocHam, lecturerAccounts[i].Id)));
            await context.SaveChangesAsync();

            // Sinh viên: tên đăng nhập và mật khẩu mặc định đều là MSSV, email MSSV@st.utc2.edu.vn
            var studentsByClass = await SeedFiles.ReadAsync<Dictionary<string, string[][]>>(contentRoot, StudentsFileName, logger);
            if (studentsByClass == null) return (lecturers.Length, 0);
            var students = studentsByClass
                .SelectMany(c => c.Value.Select(s => (Mssv: s[0], HoTen: s[1], MaLop: classes[c.Key].Id)))
                .ToArray();

            var hashes = new string[students.Length];
            Parallel.For(0, students.Length, i => hashes[i] = BCrypt.Net.BCrypt.HashPassword(students[i].Mssv, SeedWorkFactor));

            var studentAccounts = students
                .Select((s, i) => new TaiKhoan(s.Mssv, hashes[i], $"{s.Mssv}@st.utc2.edu.vn", s.HoTen, svRoleId))
                .ToList();
            context.TaiKhoans.AddRange(studentAccounts);
            await context.SaveChangesAsync();

            context.SinhViens.AddRange(students.Select((s, i) =>
                new SinhVien(s.Mssv, s.HoTen, null, null, $"{s.Mssv}@st.utc2.edu.vn", null, s.MaLop, studentAccounts[i].Id)));
            await context.SaveChangesAsync();

            return (lecturers.Length, students.Length);
        }
    }
}
