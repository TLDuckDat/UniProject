namespace UniProject.API.Data
{
    /// <summary>
    /// Đồ án môn học của lớp CQ.65.CNTT (loại DO_AN_MON_HOC, nhóm tối đa 4 sinh viên):
    /// - Công nghệ Java, HK2 2025-2026 (học kỳ 4 theo khung chương trình): đã kết thúc, đã chấm và công bố kết quả.
    ///   Nhóm và đề tài là dữ liệu mẫu tự sinh.
    /// - Lập trình trực quan, HK1 2026-2027 (học kỳ 5): đang thực hiện. Nhóm và đề tài theo bảng đăng ký của lớp,
    ///   xếp lại cho đúng tối đa 4 sinh viên/nhóm.
    /// Danh sách nhóm, nhóm cố định (giữ nguyên ở cả hai môn) và giảng viên phụ trách nằm ở Data/Seed/k65-do-an-mon-hoc.json
    /// (dữ liệu thật, không commit; mẫu ở Data/Seed.example/).
    /// </summary>
    public static class SampleK65CourseProjects
    {
        public const string ClassCode = "CQ.65.CNTT";
        public const string MajorCode = "7480201";

        public const string FileName = "k65-do-an-mon-hoc.json";

        /// <summary>Góp ý của giảng viên khi duyệt; Done = nội dung đề tài đã được bổ sung theo góp ý.</summary>
        public record Feedback(string Title, string Content, bool Done);

        public record Group(string[] Members, string Topic, string Content, string Stack, Feedback[] Feedbacks, string? OriginalTopic = null);

        public record VisualConfig(string Advisor, Group[] Groups);

        /// <summary>Committee: Chủ tịch, Thư ký, Ủy viên. HeadOfDepartment duyệt đề tài do GV ra.</summary>
        public record JavaConfig(string Advisor, string[] Committee, string HeadOfDepartment);

        /// <summary>FixedGroup: nhóm cố định ở mọi môn, trưởng nhóm đứng đầu.</summary>
        public record CourseFile(string[] FixedGroup, VisualConfig Visual, JavaConfig Java);

        // ================= Lập trình trực quan - HK1 2026-2027 =================

        public static class Visual
        {
            public const string PeriodCode = "DAMH-LTTQ-K65-2026";
            public const string PeriodName = "Đồ án môn học Lập trình trực quan - CQ.65.CNTT - HK1 2026-2027";
            public const string SchoolYear = "2026-2027";
            public const int Semester = 1;
            public const string CodePrefix = "LTTQ-K65";

            public static readonly DateTime StartDate = new(2026, 9, 7);
            public static readonly DateTime RegistrationDeadline = new(2026, 9, 30);
            public static readonly DateTime OutlineDeadline = new(2026, 10, 14);
            public static readonly DateTime ReportDeadline = new(2026, 12, 28);
            public static readonly DateTime DefenseStart = new(2027, 1, 4);
            public static readonly DateTime DefenseEnd = new(2027, 1, 8);

            public static string Objective(string topic) =>
                $"Vận dụng kiến thức Lập trình trực quan (C# WinForms, xử lý sự kiện, truy cập dữ liệu ADO.NET / Entity Framework, báo cáo) " +
                $"để xây dựng \"{topic}\" theo đúng quy trình phân tích, thiết kế, cài đặt và kiểm thử; rèn luyện kỹ năng làm việc nhóm và quản lý dự án.";

            public const string Requirements =
                "Nhóm tối đa 4 sinh viên, thành viên đầu tiên là nhóm trưởng. Xem kỹ yêu cầu chi tiết của đồ án môn học trên Moodle. " +
                "Đề tài e-commerce chỉ dành cho 3 nhóm đăng ký sớm nhất. Hạn đăng ký: hết ngày 30/09/2026. " +
                "Dùng JIRA hoặc Trello quản lý tiến độ, thêm tài khoản của giảng viên hướng dẫn vào dự án của nhóm; " +
                "xuất báo cáo tiến độ từ JIRA/Trello đưa vào cuốn báo cáo làm minh chứng làm việc nhóm. " +
                "Sản phẩm: ứng dụng desktop WinForms chạy được các chức năng đã đăng ký, cơ sở dữ liệu có dữ liệu mẫu, có phân quyền theo vai trò; " +
                "báo cáo gồm phân tích, thiết kế (use case, ERD, giao diện), cài đặt, kiểm thử, hướng dẫn sử dụng.";
        }

        // ================= Công nghệ Java - HK2 2025-2026 =================

        public static class Java
        {
            public const string PeriodCode = "DAMH-JAVA-K65-2025";
            public const string PeriodName = "Đồ án môn học Công nghệ Java - CQ.65.CNTT - HK2 2025-2026";
            public const string SchoolYear = "2025-2026";
            public const int Semester = 2;
            public const string CodePrefix = "JAVA-K65";

            public const string Room = "Phòng máy tính 2";

            public static readonly DateTime StartDate = new(2026, 1, 26);
            public static readonly DateTime RegistrationDeadline = new(2026, 2, 8);
            public static readonly DateTime OutlineDeadline = new(2026, 3, 6);
            public static readonly DateTime ReportDeadline = new(2026, 5, 22);
            public static readonly DateTime DefenseStart = new(2026, 6, 1);
            public static readonly DateTime DefenseEnd = new(2026, 6, 3);
            public static readonly DateTime PublishDate = new(2026, 6, 12);

            /// <summary>Ngày nộp và % hoàn thành của 3 lần báo cáo tiến độ.</summary>
            public static readonly (DateTime Date, int Percent, string Title, string Done, string Next)[] ProgressReports =
            [
                (new(2026, 3, 6), 25, "Báo cáo tiến độ lần 1 - Đề cương và phân tích yêu cầu",
                    "Khảo sát nghiệp vụ, đặc tả yêu cầu, vẽ sơ đồ use case, thiết kế ERD và cơ sở dữ liệu MySQL.",
                    "Thiết kế giao diện Swing, dựng khung dự án theo mô hình MVC, cài đặt lớp truy cập dữ liệu JDBC."),
                (new(2026, 4, 10), 60, "Báo cáo tiến độ lần 2 - Cài đặt chức năng chính",
                    "Hoàn thành đăng nhập, phân quyền và các chức năng quản lý danh mục, nghiệp vụ chính.",
                    "Hoàn thiện thống kê, báo cáo, xử lý ngoại lệ và kiểm thử."),
                (new(2026, 5, 15), 95, "Báo cáo tiến độ lần 3 - Hoàn thiện và kiểm thử",
                    "Hoàn thiện toàn bộ chức năng, thống kê; kiểm thử và sửa lỗi; viết báo cáo, chuẩn bị slide bảo vệ.",
                    "Chỉnh sửa báo cáo theo góp ý, đóng gói ứng dụng và nộp bản cuối."),
            ];

            public static string Objective(string topic) =>
                $"Vận dụng kiến thức Công nghệ Java (lập trình hướng đối tượng, Java Swing, JDBC, xử lý ngoại lệ, đa luồng) " +
                $"để xây dựng \"{topic}\"; rèn luyện kỹ năng phân tích, thiết kế và làm việc nhóm.";

            public const string Requirements =
                "Nhóm tối đa 4 sinh viên. Ứng dụng Java chạy được các chức năng đã đăng ký, tổ chức mã nguồn theo mô hình MVC, " +
                "cơ sở dữ liệu MySQL có dữ liệu mẫu, có đăng nhập và phân quyền. Nộp 3 lần báo cáo tiến độ. " +
                "Báo cáo gồm phân tích, thiết kế (use case, sơ đồ lớp, ERD, giao diện), cài đặt, kiểm thử, hướng dẫn sử dụng; bảo vệ trước hội đồng.";

            public const string Stack = "Java (JDK 17), Java Swing, MySQL, JDBC, mô hình MVC; NetBeans / IntelliJ IDEA, Git";
            public const string NetworkStack = "Java (JDK 17), Java Swing, Java Socket, đa luồng, MySQL, JDBC; NetBeans / IntelliJ IDEA, Git";

            /// <summary>Đề tài do giảng viên ra; đề tài đầu tiên dành cho nhóm cố định.</summary>
            public static readonly (string Topic, string Content, bool Network)[] Topics =
            [
                ("Phần mềm quản lý cửa hàng tiện lợi",
                    "Admin: quản lý tài khoản, nhân viên, danh mục hàng hóa, nhà cung cấp, bảng giá. Nhân viên bán hàng: bán hàng tại quầy, quét mã vạch, áp khuyến mãi, thanh toán, in hóa đơn, đóng ca. Nhân viên kho: nhập hàng, kiểm kê, cảnh báo hàng sắp hết và sắp hết hạn. Thống kê doanh thu theo ca, ngày, mặt hàng bán chạy.", false),
                ("Phần mềm quản lý thư viện trường học",
                    "Thủ thư: quản lý đầu sách, bản sách, tác giả, thể loại; lập phiếu mượn/trả, gia hạn, phạt quá hạn. Độc giả: tra cứu sách, xem lịch sử mượn. Admin: quản lý tài khoản, quy định mượn, thống kê sách mượn nhiều và độc giả quá hạn.", false),
                ("Phần mềm quản lý quán cà phê",
                    "Quản lý: thực đơn, bàn, nhân viên, nguyên liệu và định lượng, khuyến mãi, báo cáo doanh thu. Nhân viên: mở bàn, gọi món, chuyển/gộp bàn, thanh toán, in hóa đơn. Tự trừ nguyên liệu theo món bán, cảnh báo nguyên liệu sắp hết.", false),
                ("Phần mềm quản lý khách sạn mini",
                    "Admin: quản lý loại phòng, phòng, giá theo mùa, dịch vụ, nhân viên. Lễ tân: đặt phòng, nhận phòng, trả phòng, thêm dịch vụ, lập hóa đơn. Thống kê công suất phòng và doanh thu theo tháng.", false),
                ("Phần mềm bán vé xem phim tại quầy",
                    "Quản lý: phim, phòng chiếu, sơ đồ ghế, suất chiếu, giá vé, combo. Nhân viên: bán vé, chọn ghế trên sơ đồ, bán combo, in vé. Thống kê doanh thu theo phim, suất chiếu và tỷ lệ lấp đầy.", false),
                ("Phần mềm quản lý nhà sách",
                    "Admin: quản lý sách, tác giả, nhà xuất bản, nhà cung cấp, nhân viên. Nhân viên: nhập sách, bán sách, quản lý khách hàng thân thiết, đổi trả. Báo cáo tồn kho, doanh thu, sách bán chạy.", false),
                ("Phần mềm quản lý phòng khám nha khoa",
                    "Lễ tân: tiếp nhận bệnh nhân, đặt lịch hẹn. Bác sĩ: lập hồ sơ điều trị, chỉ định dịch vụ (cạo vôi, trám, nhổ răng...), kê đơn. Thu ngân: lập hóa đơn, thanh toán theo đợt điều trị. Thống kê lượt khám, doanh thu theo dịch vụ.", false),
                ("Phần mềm quản lý điểm sinh viên",
                    "Phòng đào tạo: quản lý khoa, lớp, sinh viên, môn học, lớp học phần. Giảng viên: nhập điểm quá trình, điểm thi. Sinh viên: tra cứu điểm, điểm trung bình tích lũy. Xếp loại học lực, in bảng điểm, thống kê kết quả theo lớp.", false),
                ("Phần mềm quản lý nhà thuốc",
                    "Admin: quản lý thuốc, nhóm thuốc, đơn vị tính, nhà cung cấp, nhân viên. Dược sĩ: nhập thuốc theo lô và hạn dùng, bán thuốc theo đơn hoặc không đơn, in hóa đơn. Cảnh báo thuốc sắp hết hạn, tồn kho thấp; báo cáo doanh thu.", false),
                ("Phần mềm quản lý cửa hàng điện máy",
                    "Quản lý sản phẩm theo hãng, danh mục, số serial; nhập hàng; bán hàng trả thẳng và trả góp; phiếu bảo hành, tiếp nhận bảo hành; quản lý khách hàng; báo cáo doanh thu, công nợ trả góp.", false),
                ("Phần mềm quản lý tiệm sửa chữa xe máy",
                    "Nhân viên tiếp nhận: lập phiếu sửa chữa theo biển số, ghi tình trạng xe. Thợ: cập nhật hạng mục sửa, phụ tùng thay thế. Thu ngân: lập hóa đơn công và phụ tùng. Quản lý kho phụ tùng, lịch sử sửa chữa từng xe, nhắc bảo dưỡng định kỳ.", false),
                ("Phần mềm quản lý ký túc xá",
                    "Quản lý tòa nhà, phòng, giường; tiếp nhận đăng ký, xếp phòng, hợp đồng lưu trú; ghi chỉ số điện nước và lập hóa đơn hàng tháng; ghi nhận vi phạm nội quy; trả phòng; thống kê phòng trống và công nợ.", false),
                ("Phần mềm quản lý trung tâm tin học",
                    "Giáo vụ: quản lý khóa học, lớp, học viên, ghi danh, thu học phí, xếp lịch học và phòng máy. Giáo viên: điểm danh, nhập điểm. Cấp chứng chỉ cho học viên đạt; thống kê sĩ số và doanh thu.", false),
                ("Phần mềm quản lý nhân sự - tiền lương",
                    "Quản lý phòng ban, chức vụ, nhân viên, hợp đồng lao động; chấm công, nghỉ phép; tính lương theo ngày công, phụ cấp, bảo hiểm, thuế TNCN; khen thưởng, kỷ luật; in bảng lương, báo cáo nhân sự.", false),
                ("Phần mềm quản lý cửa hàng giày dép",
                    "Quản lý sản phẩm theo mẫu, size, màu; nhập hàng và tồn kho theo biến thể; bán hàng, khuyến mãi theo mùa, khách hàng thân thiết; đổi size, trả hàng; báo cáo doanh thu, mẫu bán chạy.", false),
                ("Phần mềm quản lý cho thuê xe tự lái",
                    "Quản lý xe, loại xe, giá thuê theo ngày; khách hàng và giấy tờ; lập hợp đồng thuê, đặt cọc, giao xe, nhận xe, tính phí phát sinh (quá giờ, quá km, hư hỏng); lịch bảo dưỡng xe; thống kê doanh thu, tỷ lệ xe cho thuê.", false),
                ("Phần mềm quản lý dãy phòng trọ",
                    "Chủ trọ: quản lý phòng, người thuê, hợp đồng, tiền cọc; ghi chỉ số điện nước, lập hóa đơn hàng tháng, ghi nhận thanh toán; theo dõi công nợ; tiếp nhận sự cố; thống kê phòng trống và doanh thu.", false),
                ("Phần mềm quản lý cho thuê sân bóng đá mini",
                    "Quản lý sân, khung giờ, bảng giá theo giờ cao điểm; đặt sân theo lịch, đặt cố định hằng tuần; nhận cọc, thanh toán; bán nước uống, thuê đồ; thống kê doanh thu và khung giờ đông khách.", false),
                ("Phần mềm quản lý tiệm bánh",
                    "Quản lý sản phẩm, công thức và nguyên liệu; nhận đơn đặt bánh theo yêu cầu (ngày giao, ghi chú); bán tại quầy; nhập nguyên liệu, tồn kho; báo cáo doanh thu, sản phẩm bán chạy.", false),
                ("Phần mềm quản lý kho vật tư",
                    "Quản lý vật tư, đơn vị tính, kho, nhà cung cấp; phiếu nhập, phiếu xuất cho bộ phận; kiểm kê; cảnh báo tồn kho tối thiểu; báo cáo nhập - xuất - tồn theo kỳ.", false),
                ("Ứng dụng chat nội bộ doanh nghiệp",
                    "Mô hình client - server dùng Java Socket và đa luồng: đăng ký, đăng nhập; danh sách người dùng online; chat riêng, chat nhóm; gửi file, emoji; lưu lịch sử tin nhắn vào MySQL; quản trị viên quản lý tài khoản, nhóm và theo dõi kết nối.", true),
                ("Game cờ caro hai người chơi qua mạng LAN",
                    "Server quản lý phòng chơi và ghép cặp bằng Java Socket, đa luồng; người chơi đăng nhập, tạo/vào phòng, chơi cờ caro 15x15 theo lượt có giới hạn thời gian, chat trong phòng, xin hòa, đầu hàng; lưu lịch sử trận và bảng xếp hạng theo điểm.", true),
            ];
        }
    }
}
