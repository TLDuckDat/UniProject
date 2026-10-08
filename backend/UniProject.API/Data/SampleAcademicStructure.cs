namespace UniProject.API.Data
{
    /// <summary>
    /// Cơ cấu đào tạo của Phân hiệu Trường ĐH GTVT tại TP.HCM (UTC2) dùng cho dữ liệu mẫu.
    /// - Khoa / Bộ môn: Sổ tay sinh viên Khóa 64 (mục Văn phòng các Khoa và Bộ môn, Nhà E10).
    /// - Ngành / chuyên ngành: Thông tin tuyển sinh ĐHCQ năm 2025, mục II (mã tuyển sinh GSA).
    /// - Danh sách lớp K62 - K65: gtvt2.vercel.app; ký hiệu lớp: danh sách đồ án tốt nghiệp của BM CNTT và thời khóa biểu HK2 2024-2025.
    /// Việc gán ngành cho bộ môn là suy luận theo chuyên môn, trường không công bố bảng này.
    /// </summary>
    public static class SampleAcademicStructure
    {
        public record Major(string Code, string Name, (string Code, string Name)[] Specializations);
        public record Department(string Code, string Name, Major[] Majors);
        public record Faculty(string Code, string Name, Department[] Departments);
        public record ClassInfo(string Code, string Name, int Cohort, string MajorCode);

        public const string CnttDepartmentCode = "BMCNTT";
        public const string VisitingDepartmentCode = "GVTG";

        public static readonly Faculty[] Faculties =
        [
            new("KHCB", "Khoa Khoa học cơ bản",
            [
                new("LLCT", "Bộ môn Lý luận chính trị", []),
                new("NN", "Bộ môn Ngoại ngữ", []),
                new("GDTC", "Bộ môn Giáo dục thể chất", []),
                new("TOAN", "Bộ môn Toán", []),
                new("VLHH", "Bộ môn Vật lý và Hóa học", []),
            ]),
            new("CT", "Khoa Công trình",
            [
                new("CHM", "Bộ môn Cầu hầm và Metro",
                [
                    new("7580205-CLC", "Kỹ thuật XD công trình giao thông (CLC Cầu - Đường bộ Việt-Anh)", []),
                ]),
                new("DBDS", "Bộ môn Đường bộ - Đường sắt",
                [
                    new("7580205", "Kỹ thuật xây dựng công trình giao thông",
                    [
                        ("7580205-CDB", "Cầu đường bộ"),
                        ("7580205-DBGT", "Đường bộ và kỹ thuật giao thông"),
                        ("7580205-DS", "Đường sắt tốc độ cao"),
                    ]),
                ]),
                new("KTKC", "Bộ môn Kỹ thuật kết cấu", []),
                new("XDDDKT", "Bộ môn Xây dựng dân dụng và Kiến trúc",
                [
                    new("7580201", "Kỹ thuật xây dựng", [("7580201-XDDD", "Xây dựng dân dụng và công nghiệp")]),
                    new("7580101", "Kiến trúc", []),
                    new("7580106", "Quản lý đô thị và công trình", []),
                    new("7520320", "Kỹ thuật môi trường", []), // đã ngừng tuyển, còn lớp K62
                ]),
            ]),
            new("VTKT", "Khoa Vận tải - Kinh tế",
            [
                new("QLVT", "Bộ môn Quản lý vận tải",
                [
                    new("7840101", "Khai thác vận tải",
                    [
                        ("7840101-VTTM", "Vận tải - Thương mại quốc tế"),
                        ("7840101-QHGT", "Quy hoạch và quản lý GTVT đô thị"),
                    ]),
                    new("7840104", "Kinh tế vận tải", [("7840104-KTVT", "Kinh tế vận tải thủy bộ")]),
                    new("7510605", "Logistics và Quản lý chuỗi cung ứng", []),
                ]),
                new("KTXD", "Bộ môn Kinh tế xây dựng",
                [
                    new("7580301", "Kinh tế xây dựng", [("7580301-KTDT", "Kinh tế và quản lý đầu tư xây dựng")]),
                    new("7580302", "Quản lý xây dựng", [("7580302-QLDA", "Quản lý dự án")]),
                ]),
                new("TCKT", "Bộ môn Tài chính kế toán",
                [
                    new("7340201", "Tài chính - Ngân hàng", []),
                    new("7340301", "Kế toán", [("7340301-KTTH", "Kế toán tổng hợp")]),
                ]),
                new("QLKT", "Bộ môn Quản lý kinh tế",
                [
                    new("7340101", "Quản trị kinh doanh", []),
                    new("7340120", "Kinh doanh quốc tế", []),
                    new("7810103", "Quản trị dịch vụ du lịch và lữ hành", []),
                ]),
            ]),
            // Ba bộ môn trực thuộc thẳng Phân hiệu, không nằm trong khoa nào
            new("PH", "Các bộ môn trực thuộc Phân hiệu",
            [
                new("CK", "Bộ môn Cơ khí",
                [
                    new("7520114", "Kỹ thuật cơ điện tử", []),
                    new("7520116", "Kỹ thuật cơ khí động lực",
                    [
                        ("7520116-MXD", "Máy xây dựng"),
                        ("7520116-MDL", "Kỹ thuật máy động lực"),
                    ]),
                    new("7520130", "Kỹ thuật ô tô", []),
                ]),
                new("DDT", "Bộ môn Điện - Điện tử",
                [
                    new("7520201", "Kỹ thuật điện", [("7520201-TBD", "Trang bị điện trong công nghiệp và GTVT")]),
                    new("7520207", "Kỹ thuật điện tử - viễn thông",
                    [
                        ("7520207-KTVT", "Kỹ thuật viễn thông"),
                        ("7520207-DTTH", "Điện tử và tin học công nghiệp"),
                    ]),
                    new("7520216", "Kỹ thuật điều khiển và tự động hóa", [("7520216-TDH", "Tự động hóa")]),
                ]),
                new(CnttDepartmentCode, "Bộ môn Công nghệ thông tin",
                [
                    // 4 hướng chuyên sâu theo trang Giới thiệu của Bộ môn
                    new("7480201", "Công nghệ thông tin",
                    [
                        ("CNPM", "Công nghệ phần mềm"),
                        ("HTTT", "Hệ thống thông tin"),
                        ("MMT", "Mạng máy tính"),
                        ("KHMT", "Khoa học máy tính"),
                    ]),
                    new("7480106", "Kỹ thuật máy tính (vi mạch bán dẫn)", []),
                ]),
                new(VisitingDepartmentCode, "Giảng viên thỉnh giảng", []),
            ]),
        ];

        // Khóa K nhập học năm 1959 + K (K64 = 2023)
        public static int EnrollmentYear(int cohort) => 1959 + cohort;

        public static readonly ClassInfo[] Classes =
        [
            // Bộ môn CNTT: mỗi khóa một lớp; từ K62 sinh viên chọn chương trình cử nhân thì chuyển sang lớp CQ.xx.CN.CNTT
            new("CQ.61.CNTT", "Công nghệ thông tin K61", 61, "7480201"),
            new("CQ.62.CNTT", "Công nghệ thông tin K62", 62, "7480201"),
            new("CQ.62.CN.CNTT", "Cử nhân Công nghệ thông tin K62", 62, "7480201"),
            new("CQ.63.CNTT", "Công nghệ thông tin K63", 63, "7480201"),
            new("CQ.63.CN.CNTT", "Cử nhân Công nghệ thông tin K63", 63, "7480201"),
            new("CQ.64.CNTT", "Công nghệ thông tin K64", 64, "7480201"),
            new("CQ.65.CNTT", "Công nghệ thông tin K65", 65, "7480201"),
            new("CQ.66.CNTT", "Công nghệ thông tin K66", 66, "7480201"), 
            new("CQ.67.CNTT", "Công nghệ thông tin K67", 67, "7480201"), 
            // Ký hiệu đã thấy trong thời khóa biểu thì giữ nguyên, còn lại suy theo tên ngành
            new("CQ.62.CKĐL", "Kỹ thuật cơ khí động lực K62", 62, "7520116"),
            new("CQ.62.KETOAN", "Kế toán K62", 62, "7340301"), 
            new("CQ.62.KTCĐT", "Kỹ thuật cơ điện tử K62", 62, "7520114"),
            new("CQ.62.KTEXD", "Kinh tế xây dựng K62", 62, "7580301"), 
            new("CQ.62.KTMTRG", "Kỹ thuật môi trường K62", 62, "7520320"), 
            new("CQ.62.KTOTO.1", "Kỹ thuật ô tô K62 lớp 1", 62, "7520130"),
            new("CQ.62.KTOTO.2", "Kỹ thuật ô tô K62 lớp 2", 62, "7520130"),
            new("CQ.62.KTRUC", "Kiến trúc K62", 62, "7580101"), 
            new("CQ.62.KTVT", "Khai thác vận tải K62", 62, "7840101"), 
            new("CQ.62.KTXD.1", "Kỹ thuật xây dựng K62 lớp 1", 62, "7580201"),
            new("CQ.62.KTXD.2", "Kỹ thuật xây dựng K62 lớp 2", 62, "7580201"),
            new("CQ.62.KTXDCTGT.1", "Kỹ thuật xây dựng công trình giao thông K62 lớp 1", 62, "7580205"),
            new("CQ.62.KTXDCTGT.2", "Kỹ thuật xây dựng công trình giao thông K62 lớp 2", 62, "7580205"),
            new("CQ.62.KTĐ", "Kỹ thuật điện K62", 62, "7520201"), 
            new("CQ.62.KTĐTVT", "Kỹ thuật điện tử - viễn thông K62", 62, "7520207"),
            new("CQ.62.LOG", "Logistics và Quản lý chuỗi cung ứng K62", 62, "7510605"), 
            new("CQ.62.QHGT", "Quy hoạch và quản lý GTVT đô thị K62", 62, "7840101"), 
            new("CQ.62.QLXD", "Quản lý xây dựng K62", 62, "7580302"),
            new("CQ.62.QTDL&LH", "Quản trị dịch vụ du lịch và lữ hành K62", 62, "7810103"),
            new("CQ.62.QTKD", "Quản trị kinh doanh K62", 62, "7340101"), 
            new("CQ.62.TĐHĐK", "Kỹ thuật điều khiển và tự động hóa K62", 62, "7520216"),
            new("CQ.62.VTTM", "Vận tải - Thương mại quốc tế K62", 62, "7840101"), 
            new("CQ.63.CKĐL", "Kỹ thuật cơ khí động lực K63", 63, "7520116"),
            new("CQ.63.KETOAN", "Kế toán K63", 63, "7340301"), 
            new("CQ.63.KTCĐT", "Kỹ thuật cơ điện tử K63", 63, "7520114"),
            new("CQ.63.KTEXD", "Kinh tế xây dựng K63", 63, "7580301"), 
            new("CQ.63.KTOTO", "Kỹ thuật ô tô K63", 63, "7520130"),
            new("CQ.63.KTRUC", "Kiến trúc K63", 63, "7580101"), 
            new("CQ.63.KTVT", "Khai thác vận tải K63", 63, "7840101"), 
            new("CQ.63.KTXD.1", "Kỹ thuật xây dựng K63 lớp 1", 63, "7580201"),
            new("CQ.63.KTXD.2", "Kỹ thuật xây dựng K63 lớp 2", 63, "7580201"),
            new("CQ.63.KTXDCTGT.1", "Kỹ thuật xây dựng công trình giao thông K63 lớp 1", 63, "7580205"),
            new("CQ.63.KTXDCTGT.2", "Kỹ thuật xây dựng công trình giao thông K63 lớp 2", 63, "7580205"),
            new("CQ.63.KTXDCTGT.3", "Kỹ thuật xây dựng công trình giao thông K63 lớp 3", 63, "7580205"),
            new("CQ.63.KTĐ", "Kỹ thuật điện K63", 63, "7520201"), 
            new("CQ.63.KTĐTVT", "Kỹ thuật điện tử - viễn thông K63", 63, "7520207"),
            new("CQ.63.LOG", "Logistics và Quản lý chuỗi cung ứng K63", 63, "7510605"), 
            new("CQ.63.LT.KTOTO", "Liên thông - Kỹ thuật ô tô K63", 63, "7520130"), 
            new("CQ.63.QLXD", "Quản lý xây dựng K63", 63, "7580302"),
            new("CQ.63.QTDL&LH", "Quản trị dịch vụ du lịch và lữ hành K63", 63, "7810103"),
            new("CQ.63.QTKD", "Quản trị kinh doanh K63", 63, "7340101"), 
            new("CQ.63.TCNH", "Tài chính - Ngân hàng K63", 63, "7340201"), 
            new("CQ.63.TĐHĐK", "Kỹ thuật điều khiển và tự động hóa K63", 63, "7520216"),
            new("CQ.64.CKĐL", "Kỹ thuật cơ khí động lực K64", 64, "7520116"),
            new("CQ.64.KETOAN", "Kế toán K64", 64, "7340301"), 
            new("CQ.64.KTCĐT", "Kỹ thuật cơ điện tử K64", 64, "7520114"),
            new("CQ.64.KTEXD", "Kinh tế xây dựng K64", 64, "7580301"), 
            new("CQ.64.KTOTO", "Kỹ thuật ô tô K64", 64, "7520130"),
            new("CQ.64.KTRUC", "Kiến trúc K64", 64, "7580101"), 
            new("CQ.64.KTVT", "Khai thác vận tải K64", 64, "7840101"), 
            new("CQ.64.KTXD.1", "Kỹ thuật xây dựng K64 lớp 1", 64, "7580201"),
            new("CQ.64.KTXD.2", "Kỹ thuật xây dựng K64 lớp 2", 64, "7580201"),
            new("CQ.64.KTXDCTGT.1", "Kỹ thuật xây dựng công trình giao thông K64 lớp 1", 64, "7580205"),
            new("CQ.64.KTXDCTGT.2", "Kỹ thuật xây dựng công trình giao thông K64 lớp 2", 64, "7580205"),
            new("CQ.64.KTĐ", "Kỹ thuật điện K64", 64, "7520201"), 
            new("CQ.64.KTĐTVT", "Kỹ thuật điện tử - viễn thông K64", 64, "7520207"),
            new("CQ.64.LOG", "Logistics và Quản lý chuỗi cung ứng K64", 64, "7510605"), 
            new("CQ.64.QLXD", "Quản lý xây dựng K64", 64, "7580302"),
            new("CQ.64.QTDL&LH", "Quản trị dịch vụ du lịch và lữ hành K64", 64, "7810103"),
            new("CQ.64.QTKD", "Quản trị kinh doanh K64", 64, "7340101"), 
            new("CQ.64.TCNH", "Tài chính - Ngân hàng K64", 64, "7340201"), 
            new("CQ.64.TĐHĐK", "Kỹ thuật điều khiển và tự động hóa K64", 64, "7520216"),
            new("CQ.65.CKĐL", "Kỹ thuật cơ khí động lực K65", 65, "7520116"),
            new("CQ.65.KDQT", "Kinh doanh quốc tế K65", 65, "7340120"), 
            new("CQ.65.KETOAN", "Kế toán K65", 65, "7340301"), 
            new("CQ.65.KTCĐT", "Kỹ thuật cơ điện tử K65", 65, "7520114"),
            new("CQ.65.KTEXD", "Kinh tế xây dựng K65", 65, "7580301"), 
            new("CQ.65.KTOTO", "Kỹ thuật ô tô K65", 65, "7520130"),
            new("CQ.65.KTRUC", "Kiến trúc K65", 65, "7580101"), 
            new("CQ.65.KTVT", "Khai thác vận tải K65", 65, "7840101"), 
            new("CQ.65.KTXD.1", "Kỹ thuật xây dựng K65 lớp 1", 65, "7580201"),
            new("CQ.65.KTXD.2", "Kỹ thuật xây dựng K65 lớp 2", 65, "7580201"),
            new("CQ.65.KTXDCTGT.1", "Kỹ thuật xây dựng công trình giao thông K65 lớp 1", 65, "7580205"),
            new("CQ.65.KTXDCTGT.2", "Kỹ thuật xây dựng công trình giao thông K65 lớp 2", 65, "7580205"),
            new("CQ.65.KTĐ", "Kỹ thuật điện K65", 65, "7520201"), 
            new("CQ.65.KTĐTVT", "Kỹ thuật điện tử - viễn thông K65", 65, "7520207"),
            new("CQ.65.LOG", "Logistics và Quản lý chuỗi cung ứng K65", 65, "7510605"), 
            new("CQ.65.QLXD", "Quản lý xây dựng K65", 65, "7580302"),
            new("CQ.65.QTDL&LH", "Quản trị dịch vụ du lịch và lữ hành K65", 65, "7810103"),
            new("CQ.65.QTKD", "Quản trị kinh doanh K65", 65, "7340101"), 
            new("CQ.65.TCNH", "Tài chính - Ngân hàng K65", 65, "7340201"), 
            new("CQ.65.TĐHĐK", "Kỹ thuật điều khiển và tự động hóa K65", 65, "7520216"),
        ];
    }
}
