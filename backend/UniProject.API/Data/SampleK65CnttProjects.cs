namespace UniProject.API.Data
{
    /// <summary>
    /// Đợt Đồ án cơ sở lớp CQ.65.CNTT. Danh sách sinh viên - đề tài nằm ở Data/Seed/k65-do-an-co-so.json.
    /// Sinh viên cùng tên đề tài được gom thành một nhóm (quy mô Đồ án cơ sở tối đa 2 sinh viên),
    /// sinh viên xuất hiện trước trong danh sách làm trưởng nhóm.
    /// Bảng gốc không ghi giảng viên hướng dẫn, nên giảng viên cơ hữu được chia lần lượt theo thứ tự đề tài.
    ///
    /// Tham khảo Bộ môn CNTT - Phân hiệu UTC tại TP.HCM (dept.utc2.edu.vn/bomoncntt):
    /// - Mốc thời gian theo "Kế hoạch thực hiện học phần đồ án tốt nghiệp đợt giao tháng 10/2026" (đăng 16/09/2026):
    ///   phân công 05 - 07/10, nộp đề cương chậm nhất 20/10, thực hiện 12/10/2026 - 09/01/2027,
    ///   nộp báo cáo 11 - 13/01, đọc duyệt 14 - 16/01, bảo vệ dự kiến 21 - 23/01/2027.
    /// - Nội dung đề tài theo các mục của phiếu "Nhiệm vụ thiết kế" trong mẫu báo cáo của Bộ môn: mục đích - yêu cầu,
    ///   nội dung và phạm vi, công nghệ - công cụ - ngôn ngữ lập trình, kết quả dự kiến.
    /// - Công nghệ chọn theo các học phần K65 đã học đến học kỳ 5 trong khung chương trình ngành CNTT (7.48.02.01):
    ///   Thiết kế Web, Lập trình hướng đối tượng, Công nghệ Java, Cơ sở dữ liệu, Lập trình trực quan, Phân tích thiết kế hệ thống.
    /// </summary>
    public static class SampleK65CnttProjects
    {
        public const string PeriodCode = "DACS-K65-HK1-2026";
        public const string PeriodName = "Đồ án cơ sở - CQ.65.CNTT - HK1 2026-2027";
        public const string ClassCode = "CQ.65.CNTT";
        public const string MajorCode = "7480201";
        public const string SchoolYear = "2026-2027";
        public const int Semester = 1;

        public static readonly DateTime StartDate = new(2026, 10, 5);
        public static readonly DateTime RegistrationDeadline = new(2026, 10, 7);
        public static readonly DateTime OutlineDeadline = new(2026, 10, 20);
        public static readonly DateTime ReportDeadline = new(2027, 1, 13);
        public static readonly DateTime DefenseStart = new(2027, 1, 21);
        public static readonly DateTime DefenseEnd = new(2027, 1, 23);

        // Phiếu nhiệm vụ: kết quả dự kiến và yêu cầu báo cáo, dùng chung cho mọi đề tài
        public const string Requirements =
            "Sản phẩm: ứng dụng chạy được đầy đủ các chức năng trong phạm vi đề tài, cơ sở dữ liệu có dữ liệu mẫu, có phân quyền người dùng. " +
            "Nộp đề cương, kế hoạch thực hiện cho giảng viên hướng dẫn đúng hạn và báo cáo tiến độ định kỳ. " +
            "Báo cáo theo mẫu của Bộ môn CNTT: Chương 1 Mở đầu (tổng quan, mục tiêu, phạm vi); " +
            "Chương 2 Phân tích, thiết kế (use case, sơ đồ lớp / ERD, cơ sở dữ liệu, giao diện) và xây dựng ứng dụng; " +
            "Chương 3 Kết quả và kiến nghị; phụ lục hướng dẫn cài đặt, hướng dẫn sử dụng; mục lục tự động, tài liệu tham khảo.";

        public static string Objective(string purpose) =>
            $"Vận dụng kiến thức Cơ sở dữ liệu, Phân tích thiết kế hệ thống và lập trình để {purpose}. " +
            "Rèn luyện kỹ năng khảo sát nghiệp vụ, phân tích yêu cầu, làm việc nhóm và viết báo cáo kỹ thuật.";

        private const string DesignTools = "; thiết kế UML bằng StarUML / draw.io; quản lý mã nguồn bằng Git";

        public static readonly Dictionary<string, string> Stacks = new()
        {
            ["winforms"] = "C# WinForms (.NET), SQL Server, ADO.NET" + DesignTools,
            ["aspnet"] = "ASP.NET Core MVC, SQL Server, Entity Framework Core, Bootstrap" + DesignTools,
            ["php"] = "PHP (Laravel), MySQL, HTML/CSS/JavaScript, Bootstrap" + DesignTools,
            ["java"] = "Java Swing, MySQL, JDBC" + DesignTools,
            ["node"] = "Node.js (Express), ReactJS, MySQL" + DesignTools,
        };

        public record TopicDetail(string Purpose, string Scope, string Stack);

        /// <summary>Mục đích, nội dung - phạm vi và bộ công nghệ (khóa trong Stacks) của từng đề tài, theo tên đề tài.</summary>
        public static readonly Dictionary<string, TopicDetail> Topics = new()
        {
            ["Quản lý điều hành xe của bến xe miền Đông"] = new("tin học hóa việc điều hành xe xuất bến, nhập bến tại Bến xe Miền Đông",
                "Quản lý nhà xe, phương tiện, tài xế, tuyến và lịch chạy; cấp lệnh xuất bến, ghi nhận xe nhập bến; điều độ, xếp tài; thống kê lượt xe theo tuyến, theo ngày.", "winforms"),
            ["Quản lý thi giấy phép lái xe"] = new("hỗ trợ trung tâm sát hạch quản lý kỳ thi giấy phép lái xe",
                "Quản lý hồ sơ thí sinh, hạng GPLX, kỳ thi và hội đồng sát hạch; xếp lịch thi lý thuyết, sa hình, đường trường; nhập kết quả, lập danh sách đậu / rớt; thống kê theo kỳ.", "winforms"),
            ["Quản lý đồ án cơ sở, đồ án chuyên ngành"] = new("xây dựng hệ thống quản lý vòng đời đồ án cơ sở và đồ án chuyên ngành của Bộ môn",
                "Quản lý đợt đồ án, đề tài (giảng viên hoặc sinh viên đề xuất), duyệt đề tài; đăng ký nhóm, phân công giảng viên hướng dẫn; báo cáo tiến độ, nộp tài liệu; phản biện, hội đồng, lịch bảo vệ; chấm điểm và công bố kết quả.", "aspnet"),
            ["Hệ thống quản lý cửa hàng điện thoại"] = new("quản lý bán hàng và kho cho cửa hàng điện thoại di động",
                "Quản lý sản phẩm, hãng, cấu hình, số IMEI; nhập hàng từ nhà cung cấp, tồn kho; lập hóa đơn bán, bảo hành; quản lý khách hàng; báo cáo doanh thu, sản phẩm bán chạy.", "winforms"),
            ["Quản lý các tour du lịch"] = new("hỗ trợ công ty lữ hành quản lý tour và đặt tour",
                "Quản lý tour, lịch khởi hành, điểm đến, hướng dẫn viên, phương tiện; khách đặt tour và thanh toán; quản lý chi phí tour; thống kê doanh thu, số khách theo tour.", "php"),
            ["Quản lý bán vé máy bay"] = new("tin học hóa việc bán vé máy bay tại đại lý",
                "Quản lý sân bay, tuyến bay, chuyến bay, hạng ghế và giá vé; tra cứu chuyến, đặt chỗ, xuất vé, đổi / hủy vé; quản lý khách hàng; báo cáo doanh thu theo chuyến, theo tháng.", "aspnet"),
            ["Quản lý chiến dịch Mùa hè xanh"] = new("hỗ trợ Đoàn trường tổ chức và quản lý chiến dịch tình nguyện Mùa hè xanh",
                "Quản lý chiến dịch, mặt trận / địa bàn, đội hình; sinh viên đăng ký tham gia, xét duyệt; phân công công trình, phần việc; điểm danh, đánh giá, cấp giấy chứng nhận; thống kê kết quả.", "php"),
            ["Xây dựng hệ thống quản lý học viên cao học"] = new("quản lý quá trình đào tạo học viên cao học",
                "Quản lý khóa, chuyên ngành, học viên; chương trình đào tạo, lớp học phần, đăng ký học phần; điểm và học phí; đề tài luận văn, người hướng dẫn; tra cứu, thống kê.", "aspnet"),
            ["Quản lý phân công thực tập tốt nghiệp"] = new("quản lý việc phân công sinh viên đi thực tập tốt nghiệp",
                "Quản lý đợt thực tập, đơn vị thực tập, giảng viên hướng dẫn; sinh viên đăng ký nguyện vọng; phân công sinh viên - đơn vị - giảng viên; nộp nhật ký, báo cáo; nhập điểm, thống kê.", "aspnet"),
            ["Quản lý xử phạt vi phạm nồng độ cồn"] = new("hỗ trợ lực lượng chức năng quản lý các trường hợp vi phạm nồng độ cồn",
                "Quản lý cán bộ, tổ công tác, chốt kiểm tra; lập biên bản vi phạm (người, phương tiện, mức đo); áp dụng khung xử phạt theo quy định, tạm giữ phương tiện; theo dõi nộp phạt; tra cứu, thống kê.", "winforms"),
            ["Quản lý kho hàng XNK"] = new("quản lý kho hàng xuất nhập khẩu",
                "Quản lý hàng hóa, đơn vị tính, vị trí lưu kho; phiếu nhập, phiếu xuất gắn với tờ khai hải quan, đối tác; kiểm kê, cảnh báo tồn kho; báo cáo nhập - xuất - tồn.", "winforms"),
            ["Quản lý bằng cấp và chứng chỉ công dân"] = new("lưu trữ và tra cứu văn bằng, chứng chỉ của công dân",
                "Quản lý công dân, đơn vị cấp, loại văn bằng / chứng chỉ; cấp mới, cấp lại, thu hồi; tra cứu, xác minh văn bằng theo số hiệu; thống kê số lượng cấp theo đơn vị, theo năm.", "aspnet"),
            ["Quản lý hiến máu nhân đạo"] = new("hỗ trợ tổ chức và quản lý các đợt hiến máu nhân đạo",
                "Quản lý người hiến máu, nhóm máu, lịch sử hiến; đợt hiến máu, địa điểm; đăng ký tham gia, khám sàng lọc, ghi nhận lượng máu; nhắc lịch hiến lần sau; thống kê theo đợt, theo nhóm máu.", "php"),
            ["Hệ thống chẩn đoán và giải pháp khắc phục sự cố máy tính"] = new("hỗ trợ người dùng chẩn đoán và khắc phục các sự cố máy tính thường gặp",
                "Xây dựng cơ sở tri thức triệu chứng - nguyên nhân - giải pháp; chẩn đoán theo chuỗi câu hỏi và luật suy diễn; tra cứu hướng dẫn khắc phục; người dùng gửi yêu cầu hỗ trợ; quản trị viên cập nhật tri thức.", "aspnet"),
            ["Quản lý bãi giữ xe"] = new("tin học hóa việc gửi và lấy xe tại bãi giữ xe",
                "Quản lý khu vực, vị trí đỗ, loại xe và bảng giá; ghi nhận xe vào / ra theo thẻ, biển số; vé tháng; tính phí, xử lý mất thẻ; báo cáo doanh thu, lượt xe theo ca.", "winforms"),
            ["Quản lý nhà hàng"] = new("quản lý hoạt động phục vụ và bán hàng của nhà hàng",
                "Quản lý thực đơn, bàn, khu vực; đặt bàn, gọi món, chuyển / gộp bàn; in phiếu bếp, thanh toán hóa đơn; quản lý nhân viên, ca làm; nhập nguyên liệu; báo cáo doanh thu.", "winforms"),
            ["Hệ thống đặt lịch và hỗ trợ khám bệnh"] = new("cho phép bệnh nhân đặt lịch khám trực tuyến và hỗ trợ phòng khám điều phối",
                "Quản lý chuyên khoa, bác sĩ, lịch làm việc; bệnh nhân đặt / hủy lịch khám, nhận thông báo; tiếp nhận, xếp hàng đợi; ghi kết quả khám, đơn thuốc; thống kê lượt khám.", "aspnet"),
            ["Quản lý khám chữa bệnh nội trú"] = new("quản lý quá trình điều trị bệnh nhân nội trú tại bệnh viện",
                "Quản lý khoa, buồng, giường bệnh; nhập viện, chuyển khoa, xuất viện; y lệnh, thuốc, dịch vụ kỹ thuật; theo dõi diễn biến điều trị; viện phí, bảo hiểm y tế; thống kê công suất giường.", "winforms"),
            ["Quản lý khách sạn"] = new("quản lý đặt phòng và vận hành khách sạn",
                "Quản lý loại phòng, phòng, giá theo mùa; đặt phòng, nhận phòng, trả phòng; dịch vụ đi kèm; quản lý khách, nhân viên; lập hóa đơn; thống kê công suất phòng, doanh thu.", "winforms"),
            ["Quản lý tuyển dụng nhân sự cho SV CNTT"] = new("kết nối doanh nghiệp tuyển dụng với sinh viên ngành CNTT",
                "Quản lý doanh nghiệp, tin tuyển dụng, vị trí, kỹ năng yêu cầu; sinh viên tạo hồ sơ / CV, ứng tuyển; doanh nghiệp duyệt hồ sơ, hẹn phỏng vấn; Bộ môn duyệt tin; thống kê.", "node"),
            ["Hệ thống quản lý cửa hàng thời trang"] = new("quản lý bán hàng cho cửa hàng thời trang",
                "Quản lý sản phẩm theo danh mục, size, màu; nhập hàng, tồn kho theo biến thể; bán hàng tại quầy, khuyến mãi, khách hàng thân thiết; đổi trả; báo cáo doanh thu, tồn kho.", "php"),
            ["Quản lý việc làm Đồ án, LV của SV Khoa CNTT"] = new("quản lý quá trình làm đồ án, luận văn của sinh viên Khoa CNTT",
                "Quản lý đề tài, giảng viên hướng dẫn, sinh viên thực hiện; đăng ký đề tài; nộp đề cương, báo cáo tiến độ theo tuần; giảng viên nhận xét; quản lý hội đồng, điểm; tra cứu đồ án các khóa.", "aspnet"),
            ["Quản lý Đoàn viên trường GTVT"] = new("quản lý đoàn viên và hoạt động Đoàn tại Trường ĐH GTVT",
                "Quản lý chi đoàn, đoàn viên, sổ đoàn; chuyển sinh hoạt Đoàn; đoàn phí; hoạt động, phong trào và điểm rèn luyện; khen thưởng, kỷ luật; đánh giá xếp loại đoàn viên cuối năm.", "php"),
            ["Quản lý đào tạo và cấp GPLX"] = new("quản lý đào tạo lái xe và cấp giấy phép lái xe tại trung tâm",
                "Quản lý học viên, khóa học, hạng GPLX, giáo viên, xe tập; lịch học lý thuyết, thực hành; học phí; lập danh sách dự thi, nhập kết quả sát hạch; cấp và theo dõi GPLX.", "winforms"),
            ["Quản lý sinh viên một trường đại học"] = new("quản lý thông tin và kết quả học tập của sinh viên",
                "Quản lý khoa, ngành, lớp, sinh viên; môn học, lớp học phần, đăng ký học; nhập điểm, tính điểm trung bình, xếp loại học lực; tra cứu, in bảng điểm; thống kê.", "java"),
            ["Hệ thống bán vé xe Bến xe Miền Đông"] = new("bán vé xe khách trực tuyến và tại quầy cho các tuyến xuất phát từ Bến xe Miền Đông",
                "Quản lý nhà xe, tuyến, chuyến, sơ đồ ghế; tra cứu chuyến, chọn ghế, đặt vé, thanh toán; in vé / vé điện tử; hủy, đổi vé; báo cáo doanh thu theo nhà xe, theo tuyến.", "aspnet"),
            ["Quản lý quán cà phê"] = new("quản lý bán hàng cho quán cà phê",
                "Quản lý thực đơn, bàn; gọi món, thanh toán, in hóa đơn; quản lý nguyên liệu và định lượng; nhân viên, chấm công theo ca; khuyến mãi; báo cáo doanh thu theo ngày, theo món.", "winforms"),
            ["Hệ thống e-learning hỗ trợ học tập, giảng dạy từ xa"] = new("hỗ trợ giảng viên và sinh viên dạy - học trực tuyến",
                "Quản lý khóa học, bài giảng (video, tài liệu), lớp học; sinh viên ghi danh, học theo tiến độ; bài tập, bài kiểm tra trắc nghiệm, chấm điểm; diễn đàn hỏi đáp; thống kê tiến độ học.", "node"),
            ["Hệ thống quản lý SV 5 tốt các cấp"] = new("quản lý việc xét chọn danh hiệu Sinh viên 5 tốt các cấp",
                "Quản lý năm học, cấp xét (Khoa, Trường, Thành phố, Trung ương) và tiêu chí; sinh viên nộp hồ sơ, minh chứng; hội đồng các cấp xét duyệt; công bố kết quả; thống kê theo lớp, theo cấp.", "php"),
            ["Quản lý giải vô địch quốc gia"] = new("quản lý tổ chức một giải bóng đá vô địch quốc gia",
                "Quản lý mùa giải, đội bóng, cầu thủ, sân vận động, trọng tài; lập lịch thi đấu; nhập kết quả, bàn thắng, thẻ phạt; bảng xếp hạng, danh sách vua phá lưới; tra cứu.", "java"),
            ["Quản lý công dân trong nền công nghiệp 4.0"] = new("quản lý thông tin công dân và thủ tục hành chính cơ bản theo hướng số hóa",
                "Quản lý hộ, nhân khẩu, định danh công dân; khai sinh, tạm trú, tạm vắng, chuyển hộ khẩu; công dân nộp hồ sơ trực tuyến, theo dõi trạng thái; cán bộ xử lý; thống kê dân cư.", "aspnet"),
            ["Quản lý thư viện"] = new("quản lý tài liệu và mượn - trả sách tại thư viện",
                "Quản lý đầu sách, bản sách, tác giả, thể loại, nhà xuất bản; độc giả, thẻ thư viện; mượn, trả, gia hạn, phạt quá hạn; tra cứu sách; thống kê sách mượn nhiều, độc giả quá hạn.", "java"),
            ["Quản lý sinh viên ngoại trú"] = new("quản lý thông tin sinh viên ở ngoại trú",
                "Quản lý sinh viên, nơi trọ, chủ trọ, địa bàn; sinh viên khai báo và cập nhật nơi ở; nhà trường xác nhận, kiểm tra định kỳ; liên hệ địa phương; thống kê theo địa bàn, theo lớp.", "php"),
            ["Quản lý Kiosk căn tin trường học"] = new("xây dựng kiosk tự phục vụ gọi món cho căn tin trường học",
                "Quản lý quầy, món ăn, combo, giá; sinh viên gọi món tại kiosk, thanh toán bằng ví / mã QR; quầy nhận và xác nhận đơn, thông báo lấy món; báo cáo doanh thu theo quầy, theo món.", "node"),
            ["Phần mềm quản lý và liên lạc cựu sinh viên của Khoa"] = new("duy trì liên lạc giữa Khoa và cựu sinh viên",
                "Quản lý cựu sinh viên theo khóa, nơi công tác, chức vụ; cựu sinh viên cập nhật thông tin; tin tức, sự kiện, đăng ký tham dự; gửi thông báo / email theo nhóm; thống kê việc làm.", "aspnet"),
            ["Quản lý nghiên cứu khoa học của giảng viên GTVT"] = new("quản lý hoạt động nghiên cứu khoa học của giảng viên Trường ĐH GTVT",
                "Quản lý giảng viên, đề tài các cấp, bài báo, hội thảo; đăng ký đề tài, xét duyệt, nghiệm thu; tính giờ nghiên cứu khoa học; minh chứng; báo cáo thống kê theo bộ môn, theo năm.", "aspnet"),
            ["Quản lý bệnh án cá nhân"] = new("lưu trữ và tra cứu hồ sơ sức khỏe, bệnh án của cá nhân",
                "Quản lý hồ sơ cá nhân, tiền sử bệnh, dị ứng; lần khám, chẩn đoán, đơn thuốc, kết quả xét nghiệm; nhắc lịch uống thuốc, tái khám; chia sẻ bệnh án cho bác sĩ có thời hạn.", "node"),
            ["Hệ thống quản lý và chăm sóc khách hàng"] = new("quản lý quan hệ và chăm sóc khách hàng (CRM) cho doanh nghiệp",
                "Quản lý khách hàng, phân nhóm; lịch sử giao dịch, cơ hội bán hàng; ghi nhận yêu cầu hỗ trợ, phân công xử lý; chương trình chăm sóc, nhắc lịch, gửi email; báo cáo hiệu quả.", "aspnet"),
            ["Quản lý học sinh một trường phổ thông"] = new("quản lý học sinh và kết quả học tập tại trường phổ thông",
                "Quản lý năm học, khối, lớp, học sinh, giáo viên chủ nhiệm, phân công giảng dạy; nhập điểm theo môn, tính điểm trung bình; xếp loại học lực, hạnh kiểm; sổ liên lạc; thống kê.", "java"),
            ["Phần mềm quản lý hoạt động của CB-GV-NV Khoa"] = new("quản lý hồ sơ và hoạt động của cán bộ, giảng viên, nhân viên trong Khoa",
                "Quản lý hồ sơ nhân sự, chức vụ, học hàm học vị; lịch công tác, phân công giảng dạy; khối lượng giờ giảng; nghỉ phép; khen thưởng, đánh giá cuối năm; thống kê.", "aspnet"),
            ["Quản lý tiêu hao vật tư sản xuất"] = new("theo dõi định mức và tiêu hao vật tư trong sản xuất",
                "Quản lý vật tư, định mức theo sản phẩm, phân xưởng; lệnh sản xuất; cấp phát vật tư, ghi nhận tiêu hao thực tế; so sánh với định mức, cảnh báo vượt mức; báo cáo theo kỳ.", "winforms"),
            ["Quản lý các CLB, đội, nhóm trường GTVT"] = new("quản lý các câu lạc bộ, đội, nhóm sinh viên tại Trường ĐH GTVT",
                "Quản lý CLB, ban chủ nhiệm, thành viên; sinh viên đăng ký tham gia, CLB duyệt; kế hoạch, hoạt động, điểm danh; kinh phí; Đoàn - Hội duyệt hoạt động; đánh giá, thống kê.", "php"),
            ["Quản lý ký túc xá sinh viên"] = new("quản lý việc ở nội trú của sinh viên tại ký túc xá",
                "Quản lý tòa nhà, phòng, giường; sinh viên đăng ký ở, xét duyệt, xếp phòng; hợp đồng, phí lưu trú, điện nước; vi phạm nội quy; trả phòng; thống kê phòng trống, công nợ.", "aspnet"),
            ["Phần mềm quản lý đào tạo của Khoa"] = new("quản lý kế hoạch và tổ chức đào tạo tại Khoa",
                "Quản lý chương trình đào tạo, học phần; kế hoạch đào tạo theo học kỳ; mở lớp học phần, phân công giảng viên; thời khóa biểu, phòng học; lịch thi; theo dõi khối lượng giảng dạy.", "aspnet"),
            ["Hệ thống quản lý phòng gym"] = new("quản lý hội viên và hoạt động của phòng tập gym",
                "Quản lý gói tập, hội viên, thẻ tập; đăng ký, gia hạn gói; check-in ra vào; huấn luyện viên và lịch tập cá nhân; thiết bị; thanh toán; báo cáo doanh thu, hội viên sắp hết hạn.", "winforms"),
            ["Quản lý bảo trì và sửa chữa thiết bị"] = new("quản lý thiết bị và công tác bảo trì, sửa chữa",
                "Quản lý thiết bị, vị trí, đơn vị sử dụng; lập kế hoạch bảo trì định kỳ; báo hỏng, phân công kỹ thuật viên, theo dõi sửa chữa; linh kiện thay thế, chi phí; lịch sử và thống kê.", "aspnet"),
            ["Quản lý thái độ học tập của SV"] = new("theo dõi chuyên cần và thái độ học tập của sinh viên",
                "Quản lý lớp học phần, buổi học; điểm danh (mã QR / thủ công); ghi nhận phát biểu, nộp bài, vi phạm; tính điểm chuyên cần; cảnh báo sinh viên vắng nhiều; cố vấn học tập theo dõi, thống kê.", "aspnet"),
            ["Quản lý nhân sự của một công ty"] = new("quản lý nhân sự và tiền lương cho doanh nghiệp",
                "Quản lý phòng ban, chức vụ, nhân viên, hợp đồng lao động; chấm công, nghỉ phép; tính lương, phụ cấp, bảo hiểm; khen thưởng, kỷ luật; báo cáo nhân sự, bảng lương.", "winforms"),
            ["Hệ thống đặt vé xem phim online"] = new("cho phép khách hàng đặt vé xem phim trực tuyến",
                "Quản lý phim, rạp, phòng chiếu, sơ đồ ghế, suất chiếu; khách tra cứu, chọn ghế, đặt vé, thanh toán, nhận vé QR; combo bắp nước; khuyến mãi; báo cáo doanh thu theo phim.", "node"),
            ["Quản lý Tín dụng ngân hàng"] = new("quản lý hoạt động cho vay tín dụng tại ngân hàng",
                "Quản lý khách hàng, sản phẩm vay, lãi suất; tiếp nhận hồ sơ vay, thẩm định, phê duyệt; giải ngân; lịch trả nợ gốc - lãi, thu nợ, nợ quá hạn; báo cáo dư nợ, phân loại nợ.", "aspnet"),
        };

        /// <summary>Data/Seed/k65-do-an-co-so.json: { "assignments": [{ "mssv": "...", "topic": "..." }] } (dữ liệu thật, không commit).</summary>
        public const string FileName = "k65-do-an-co-so.json";
        public record Assignment(string Mssv, string Topic);
        public record AssignmentFile(Assignment[] Assignments);
    }
}
