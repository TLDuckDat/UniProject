using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoSo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCoSo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenCoSo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConHoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoSo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HocPhan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenHP = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoTC = table.Column<int>(type: "int", nullable: false),
                    LoaiHP = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TinhDiem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaRuleSet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenRuleSet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CanCuPhapLy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MaxTCChuaTichLuy = table.Column<int>(type: "int", nullable: false),
                    GpaBaoVeMin = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    GpaTotNghiepMin = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    DiemDatDATN = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    TrongSoQT = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    HeSoThoiGianToiDa = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    MinTVHoiDong = table.Column<int>(type: "int", nullable: false),
                    MaxTVHoiDong = table.Column<int>(type: "int", nullable: false),
                    NguongHaHangHocLaiPct = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    HaHangKhiKyLuat = table.Column<bool>(type: "bit", nullable: false),
                    HieuLucTu = table.Column<DateOnly>(type: "date", nullable: false),
                    HieuLucDen = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaVaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenVaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DotDATN",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    MaDot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenDot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NamHoc = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    HocKy = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    SoTuan = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayNopQuyenHan = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotDATN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotDATN_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Khoa",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    MaKhoa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenKhoa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KhoiNganh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Khoa_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhongBaoVe",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    TenPhong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SucChua = table.Column<int>(type: "int", nullable: false),
                    ConSuDung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBaoVe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhongBaoVe_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyetDinh",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    SoQuyetDinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayKy = table.Column<DateOnly>(type: "date", nullable: false),
                    LoaiQD = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NguoiKy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileScanId = table.Column<long>(type: "bigint", nullable: true),
                    DaKhoaDuLieu = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyetDinh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuyetDinh_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleSetKhoaHoc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleSetId = table.Column<long>(type: "bigint", nullable: false),
                    KhoaHocTu = table.Column<int>(type: "int", nullable: false),
                    KhoaHocDen = table.Column<int>(type: "int", nullable: true),
                    CanXacNhan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSetKhoaHoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleSetKhoaHoc_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MocTienDo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotId = table.Column<long>(type: "bigint", nullable: false),
                    MaMoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TuanTu = table.Column<byte>(type: "tinyint", nullable: false),
                    TuanDen = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayHan = table.Column<DateOnly>(type: "date", nullable: false),
                    YeuCauToiThieuPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MocTienDo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MocTienDo_DotDATN_DotId",
                        column: x => x.DotId,
                        principalTable: "DotDATN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoMon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaId = table.Column<long>(type: "bigint", nullable: false),
                    MaBM = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenBM = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoMon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoMon_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChuongTrinhDaoTao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaId = table.Column<long>(type: "bigint", nullable: false),
                    MaCTDT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenCTDT = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nganh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    He = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChuongTrinh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TongTCYeuCau = table.Column<int>(type: "int", nullable: false),
                    TCDATN = table.Column<int>(type: "int", nullable: false),
                    TCTTTN = table.Column<int>(type: "int", nullable: false),
                    CoHocPhanThayThe = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuongTrinhDaoTao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChuongTrinhDaoTao_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThamSoDaoVan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaId = table.Column<long>(type: "bigint", nullable: false),
                    RuleSetId = table.Column<long>(type: "bigint", nullable: true),
                    ChuongTrinh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Bat = table.Column<bool>(type: "bit", nullable: false),
                    NguongPhanTram = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThamSoDaoVan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThamSoDaoVan_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThamSoDaoVan_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoiDong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotId = table.Column<long>(type: "bigint", nullable: false),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    KhoaId = table.Column<long>(type: "bigint", nullable: false),
                    MaHD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenHD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HinhThuc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LinkGhiHinh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuyetDinhId = table.Column<long>(type: "bigint", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoiDong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoiDong_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoiDong_DotDATN_DotId",
                        column: x => x.DotId,
                        principalTable: "DotDATN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoiDong_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoiDong_QuyetDinh_QuyetDinhId",
                        column: x => x.QuyetDinhId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiangVien",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoMonId = table.Column<long>(type: "bigint", nullable: false),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    MaGV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HocHam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HocVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LoaiGV = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuotaToiDa = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ConHoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiangVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiangVien_BoMon_BoMonId",
                        column: x => x.BoMonId,
                        principalTable: "BoMon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiangVien_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThamSoBoMon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoMonId = table.Column<long>(type: "bigint", nullable: false),
                    RuleSetId = table.Column<long>(type: "bigint", nullable: false),
                    TrongSoQT = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    TrongSoHD = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    TachDiemKhac = table.Column<bool>(type: "bit", nullable: false),
                    SoNgaySuaGapToiDa = table.Column<byte>(type: "tinyint", nullable: false),
                    DungHanKhiNghiTamThoi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThamSoBoMon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThamSoBoMon_BoMon_BoMonId",
                        column: x => x.BoMonId,
                        principalTable: "BoMon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThamSoBoMon_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CTDTHocPhan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CTDTId = table.Column<long>(type: "bigint", nullable: false),
                    HocPhanId = table.Column<long>(type: "bigint", nullable: false),
                    HocKyKeHoach = table.Column<int>(type: "int", nullable: false),
                    LaHPThayTheDATN = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CTDTHocPhan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CTDTHocPhan_ChuongTrinhDaoTao_CTDTId",
                        column: x => x.CTDTId,
                        principalTable: "ChuongTrinhDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CTDTHocPhan_HocPhan_HocPhanId",
                        column: x => x.HocPhanId,
                        principalTable: "HocPhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SinhVien",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                    KhoaHoc = table.Column<int>(type: "int", nullable: false),
                    CTDTId = table.Column<long>(type: "bigint", nullable: false),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    TrangThaiHocTap = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayNhapHoc = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinhVien_ChuongTrinhDaoTao_CTDTId",
                        column: x => x.CTDTId,
                        principalTable: "ChuongTrinhDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinhVien_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThanhVienHoiDong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiDongId = table.Column<long>(type: "bigint", nullable: false),
                    GiangVienId = table.Column<long>(type: "bigint", nullable: false),
                    ChucDanh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CoMat = table.Column<bool>(type: "bit", nullable: false),
                    DongYTrucTuyen = table.Column<bool>(type: "bit", nullable: true),
                    ThayTheChoId = table.Column<long>(type: "bigint", nullable: true),
                    LyDoThayThe = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhVienHoiDong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThanhVienHoiDong_GiangVien_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThanhVienHoiDong_HoiDong_HoiDongId",
                        column: x => x.HoiDongId,
                        principalTable: "HoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThanhVienHoiDong_ThanhVienHoiDong_ThayTheChoId",
                        column: x => x.ThayTheChoId,
                        principalTable: "ThanhVienHoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BangDiem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: false),
                    HocPhanId = table.Column<long>(type: "bigint", nullable: false),
                    HocKy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LanHoc = table.Column<int>(type: "int", nullable: false),
                    Diem10 = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    DiemChu = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Diem4 = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: true),
                    TrangThaiDiem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NguonDongBo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DongBoLuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangDiem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BangDiem_HocPhan_HocPhanId",
                        column: x => x.HocPhanId,
                        principalTable: "HocPhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BangDiem_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KyLuatSV",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: false),
                    MucKyLuat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TuNgay = table.Column<DateOnly>(type: "date", nullable: false),
                    DenNgay = table.Column<DateOnly>(type: "date", nullable: true),
                    DinhChiHocTap = table.Column<bool>(type: "bit", nullable: false),
                    TruyCuuHinhSu = table.Column<bool>(type: "bit", nullable: false),
                    NguonCTSV = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KyLuatSV", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KyLuatSV_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GiangVienId = table.Column<long>(type: "bigint", nullable: true),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: true),
                    LoaiTK = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConHoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_GiangVien_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinTienDoSV",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: false),
                    KyCuoiKeHoach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HanToiDa = table.Column<DateOnly>(type: "date", nullable: false),
                    DungTienDo = table.Column<bool>(type: "bit", nullable: false),
                    DongBoLuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinTienDoSV", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongTinTienDoSV_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiDungId = table.Column<long>(type: "bigint", nullable: true),
                    CoSoId = table.Column<long>(type: "bigint", nullable: true),
                    LoaiDoiTuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DoiTuongId = table.Column<long>(type: "bigint", nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GiaTriCu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaTriMoi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoQDBatBuoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_TaiKhoan_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeTai",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotId = table.Column<long>(type: "bigint", nullable: false),
                    BoMonId = table.Column<long>(type: "bigint", nullable: false),
                    TenDeTai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoaiDeTai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaxSV = table.Column<byte>(type: "tinyint", nullable: false),
                    NguonDeXuat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GVDeXuatId = table.Column<long>(type: "bigint", nullable: true),
                    SVDeXuatId = table.Column<long>(type: "bigint", nullable: true),
                    TrangThaiDuyet = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NguoiDuyetId = table.Column<long>(type: "bigint", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanhBaoTrung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeTai", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeTai_BoMon_BoMonId",
                        column: x => x.BoMonId,
                        principalTable: "BoMon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeTai_DotDATN_DotId",
                        column: x => x.DotId,
                        principalTable: "DotDATN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeTai_GiangVien_GVDeXuatId",
                        column: x => x.GVDeXuatId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeTai_SinhVien_SVDeXuatId",
                        column: x => x.SVDeXuatId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeTai_TaiKhoan_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanVaiTro",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<long>(type: "bigint", nullable: false),
                    VaiTroId = table.Column<long>(type: "bigint", nullable: false),
                    CoSoId = table.Column<long>(type: "bigint", nullable: true),
                    KhoaId = table.Column<long>(type: "bigint", nullable: true),
                    BoMonId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanVaiTro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_BoMon_BoMonId",
                        column: x => x.BoMonId,
                        principalTable: "BoMon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_VaiTro_VaiTroId",
                        column: x => x.VaiTroId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SVTrongDot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotId = table.Column<long>(type: "bigint", nullable: false),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: false),
                    CTDTId = table.Column<long>(type: "bigint", nullable: false),
                    CoSoId = table.Column<long>(type: "bigint", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CoNoTinChi = table.Column<bool>(type: "bit", nullable: false),
                    ChuaQuaTTTN = table.Column<bool>(type: "bit", nullable: false),
                    DeTaiId = table.Column<long>(type: "bigint", nullable: true),
                    QuyetDinhGiaoId = table.Column<long>(type: "bigint", nullable: true),
                    LanLamLai = table.Column<int>(type: "int", nullable: false),
                    HoSoTruocId = table.Column<long>(type: "bigint", nullable: true),
                    NgayHanHoanBaoVe = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SVTrongDot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_ChuongTrinhDaoTao_CTDTId",
                        column: x => x.CTDTId,
                        principalTable: "ChuongTrinhDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_CoSo_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_DeTai_DeTaiId",
                        column: x => x.DeTaiId,
                        principalTable: "DeTai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_DotDATN_DotId",
                        column: x => x.DotId,
                        principalTable: "DotDATN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_QuyetDinh_QuyetDinhGiaoId",
                        column: x => x.QuyetDinhGiaoId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_SVTrongDot_HoSoTruocId",
                        column: x => x.HoSoTruocId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SVTrongDot_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaoCaoTienDo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    MocTienDoId = table.Column<long>(type: "bigint", nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: true),
                    TyLeHoanThanhPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GVHDNhanXet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiemTienDo = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    KetQua = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CanhBaoGuiLuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoTienDo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaoCaoTienDo_MocTienDo_MocTienDoId",
                        column: x => x.MocTienDoId,
                        principalTable: "MocTienDo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaoCaoTienDo_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DangKyNguyenVong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    DeTaiId = table.Column<long>(type: "bigint", nullable: false),
                    ThuTuNguyenVong = table.Column<byte>(type: "tinyint", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyNguyenVong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DangKyNguyenVong_DeTai_DeTaiId",
                        column: x => x.DeTaiId,
                        principalTable: "DeTai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DangKyNguyenVong_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiemDATN",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    RuleSetId = table.Column<long>(type: "bigint", nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiemGVHD = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    DiemGVDD = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    DiemKhac = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    DiemQuaTrinh = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    DiemBaoVe = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    TrongSoQT = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    TrongSoHD = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    DiemDATN = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    DiemChu = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DiemHe4 = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: false),
                    Dat = table.Column<bool>(type: "bit", nullable: false),
                    ChotLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChotBoiId = table.Column<long>(type: "bigint", nullable: true),
                    QuyetDinhSuaDiemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDATN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiemDATN_QuyetDinh_QuyetDinhSuaDiemId",
                        column: x => x.QuyetDinhSuaDiemId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiemDATN_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiemDATN_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiemDATN_TaiKhoan_ChotBoiId",
                        column: x => x.ChotBoiId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonTu",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    LoaiDon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileMinhChungId = table.Column<long>(type: "bigint", nullable: true),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NguoiXuLyId = table.Column<long>(type: "bigint", nullable: true),
                    KetQuaXuLy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HanMoi = table.Column<DateOnly>(type: "date", nullable: true),
                    QuyetDinhId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonTu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonTu_QuyetDinh_QuyetDinhId",
                        column: x => x.QuyetDinhId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonTu_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonTu_TaiKhoan_NguoiXuLyId",
                        column: x => x.NguoiXuLyId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichBaoVe",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiDongId = table.Column<long>(type: "bigint", nullable: false),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    PhongBaoVeId = table.Column<long>(type: "bigint", nullable: false),
                    NgayBaoVe = table.Column<DateOnly>(type: "date", nullable: false),
                    Ca = table.Column<byte>(type: "tinyint", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TruDiemViPhamPct = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichBaoVe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichBaoVe_HoiDong_HoiDongId",
                        column: x => x.HoiDongId,
                        principalTable: "HoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichBaoVe_PhongBaoVe_PhongBaoVeId",
                        column: x => x.PhongBaoVeId,
                        principalTable: "PhongBaoVe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichBaoVe_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichNhacViec",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    LoaiNhac = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HanNgay = table.Column<DateOnly>(type: "date", nullable: false),
                    DaGui = table.Column<bool>(type: "bit", nullable: false),
                    GuiLuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichNhacViec", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichNhacViec_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuTrangThai",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    TuTrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DenTrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiThucHienId = table.Column<long>(type: "bigint", nullable: true),
                    QuyetDinhId = table.Column<long>(type: "bigint", nullable: true),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuTrangThai", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuTrangThai_QuyetDinh_QuyetDinhId",
                        column: x => x.QuyetDinhId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuTrangThai_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuTrangThai_TaiKhoan_NguoiThucHienId",
                        column: x => x.NguoiThucHienId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhanCong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    GiangVienId = table.Column<long>(type: "bigint", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: true),
                    LyDoThayDoi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    QuyetDinhId = table.Column<long>(type: "bigint", nullable: true),
                    DuocNhapDiem = table.Column<bool>(type: "bit", nullable: false),
                    TuKhaiXungDot = table.Column<bool>(type: "bit", nullable: false),
                    VuotQuotaDuyetBoiId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhanCong_GiangVien_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCong_QuyetDinh_QuyetDinhId",
                        column: x => x.QuyetDinhId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCong_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCong_TaiKhoan_VuotQuotaDuyetBoiId",
                        column: x => x.VuotQuotaDuyetBoiId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhienBanQuyen",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    SoPhienBan = table.Column<int>(type: "int", nullable: false),
                    LoaiBan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    HashSHA256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    KichThuocByte = table.Column<long>(type: "bigint", nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NopQuaHan = table.Column<bool>(type: "bit", nullable: false),
                    LaBanBaoVe = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhienBanQuyen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhienBanQuyen_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuGiaoNhiemVu",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    NoiDungNhiemVu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YeuCauSanPham = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayGiao = table.Column<DateOnly>(type: "date", nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuGiaoNhiemVu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuGiaoNhiemVu_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XetDieuKien",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    LoaiXet = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RuleSetId = table.Column<long>(type: "bigint", nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThoiDiemXet = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiChayId = table.Column<long>(type: "bigint", nullable: true),
                    KetQua = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TCChuaTichLuy = table.Column<int>(type: "int", nullable: false),
                    GpaTichLuy = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    K1_KyLuat = table.Column<bool>(type: "bit", nullable: true),
                    K2_GVHD = table.Column<bool>(type: "bit", nullable: true),
                    K3_GVDD = table.Column<bool>(type: "bit", nullable: true),
                    K4_DuTC = table.Column<bool>(type: "bit", nullable: true),
                    K5_Gpa = table.Column<bool>(type: "bit", nullable: true),
                    K6_TTTN = table.Column<bool>(type: "bit", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XetDieuKien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XetDieuKien_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XetDieuKien_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XetDieuKien_TaiKhoan_NguoiChayId",
                        column: x => x.NguoiChayId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YKienGVHD",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    GiangVienId = table.Column<long>(type: "bigint", nullable: false),
                    DongY = table.Column<bool>(type: "bit", nullable: false),
                    LyDoKhongDongY = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiemGVHD = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    DiemKhac = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    NgayCho = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YKienGVHD", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YKienGVHD_GiangVien_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YKienGVHD_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BienBan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LichBaoVeId = table.Column<long>(type: "bigint", nullable: false),
                    ThuKyId = table.Column<long>(type: "bigint", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: true),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChenhLechMax = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    ChuTichXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    XacNhanLuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienBan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BienBan_LichBaoVe_LichBaoVeId",
                        column: x => x.LichBaoVeId,
                        principalTable: "LichBaoVe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienBan_ThanhVienHoiDong_ThuKyId",
                        column: x => x.ThuKyId,
                        principalTable: "ThanhVienHoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuDiem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LichBaoVeId = table.Column<long>(type: "bigint", nullable: false),
                    ThanhVienHoiDongId = table.Column<long>(type: "bigint", nullable: false),
                    Diem = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NhapHo = table.Column<bool>(type: "bit", nullable: false),
                    NguoiNhapId = table.Column<long>(type: "bigint", nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaNop = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuDiem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuDiem_LichBaoVe_LichBaoVeId",
                        column: x => x.LichBaoVeId,
                        principalTable: "LichBaoVe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDiem_TaiKhoan_NguoiNhapId",
                        column: x => x.NguoiNhapId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDiem_ThanhVienHoiDong_ThanhVienHoiDongId",
                        column: x => x.ThanhVienHoiDongId,
                        principalTable: "ThanhVienHoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyetDinhDoiTuong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuyetDinhId = table.Column<long>(type: "bigint", nullable: false),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: true),
                    HoiDongId = table.Column<long>(type: "bigint", nullable: true),
                    PhanCongId = table.Column<long>(type: "bigint", nullable: true),
                    DiemDATNId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyetDinhDoiTuong", x => x.Id);
                    table.CheckConstraint("CK_QuyetDinhDoiTuong_MotDoiTuong", "(CASE WHEN [SVTrongDotId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [HoiDongId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PhanCongId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [DiemDATNId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_QuyetDinhDoiTuong_DiemDATN_DiemDATNId",
                        column: x => x.DiemDATNId,
                        principalTable: "DiemDATN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyetDinhDoiTuong_HoiDong_HoiDongId",
                        column: x => x.HoiDongId,
                        principalTable: "HoiDong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyetDinhDoiTuong_PhanCong_PhanCongId",
                        column: x => x.PhanCongId,
                        principalTable: "PhanCong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyetDinhDoiTuong_QuyetDinh_QuyetDinhId",
                        column: x => x.QuyetDinhId,
                        principalTable: "QuyetDinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyetDinhDoiTuong_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaDocDuyet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SVTrongDotId = table.Column<long>(type: "bigint", nullable: false),
                    PhienBanQuyenId = table.Column<long>(type: "bigint", nullable: false),
                    GVDDId = table.Column<long>(type: "bigint", nullable: false),
                    VongDoc = table.Column<byte>(type: "tinyint", nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HanSuaDen = table.Column<DateOnly>(type: "date", nullable: true),
                    DiemGVDD = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    NgayQuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaDocDuyet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KetQuaDocDuyet_GiangVien_GVDDId",
                        column: x => x.GVDDId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KetQuaDocDuyet_PhienBanQuyen_PhienBanQuyenId",
                        column: x => x.PhienBanQuyenId,
                        principalTable: "PhienBanQuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KetQuaDocDuyet_SVTrongDot_SVTrongDotId",
                        column: x => x.SVTrongDotId,
                        principalTable: "SVTrongDot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaKiemTraDaoVan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhienBanQuyenId = table.Column<long>(type: "bigint", nullable: false),
                    PhanMemQuet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TyLeTrungPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NguongApDungPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VuotNguong = table.Column<bool>(type: "bit", nullable: false),
                    FileBaoCaoId = table.Column<long>(type: "bigint", nullable: true),
                    NgayQuet = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaKiemTraDaoVan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KetQuaKiemTraDaoVan_PhienBanQuyen_PhienBanQuyenId",
                        column: x => x.PhienBanQuyenId,
                        principalTable: "PhienBanQuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LuuTruThuVien",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhienBanQuyenId = table.Column<long>(type: "bigint", nullable: false),
                    NguoiNhanId = table.Column<long>(type: "bigint", nullable: false),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaLuuTru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GVHDXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    NgayGVHDXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuuTruThuVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LuuTruThuVien_PhienBanQuyen_PhienBanQuyenId",
                        column: x => x.PhienBanQuyenId,
                        principalTable: "PhienBanQuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LuuTruThuVien_TaiKhoan_NguoiNhanId",
                        column: x => x.NguoiNhanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XetDieuKienChiTiet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    XetDieuKienId = table.Column<long>(type: "bigint", nullable: false),
                    HocPhanId = table.Column<long>(type: "bigint", nullable: false),
                    SoTC = table.Column<int>(type: "int", nullable: false),
                    LoaiNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BiLoaiTru = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XetDieuKienChiTiet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XetDieuKienChiTiet_HocPhan_HocPhanId",
                        column: x => x.HocPhanId,
                        principalTable: "HocPhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XetDieuKienChiTiet_XetDieuKien_XetDieuKienId",
                        column: x => x.XetDieuKienId,
                        principalTable: "XetDieuKien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CoSo",
                columns: new[] { "Id", "ConHoatDong", "MaCoSo", "TenCoSo" },
                values: new object[,]
                {
                    { 1L, true, "HN", "Trường ĐH Giao thông Vận tải (UTC) – Hà Nội" },
                    { 2L, true, "HCM", "Phân hiệu TP. Hồ Chí Minh (UTC2)" }
                });

            migrationBuilder.InsertData(
                table: "RuleSet",
                columns: new[] { "Id", "CanCuPhapLy", "DiemDatDATN", "GpaBaoVeMin", "GpaTotNghiepMin", "HaHangKhiKyLuat", "HeSoThoiGianToiDa", "HieuLucDen", "HieuLucTu", "MaRuleSet", "MaxTCChuaTichLuy", "MaxTVHoiDong", "MinTVHoiDong", "NguongHaHangHocLaiPct", "TenRuleSet", "TrongSoQT" },
                values: new object[,]
                {
                    { 1L, "QĐ 2006/QĐ-ĐHGTVT (nền Thông tư 08/2021/TT-BGDĐT)", 5.5m, 1.90m, 2.00m, true, 2.0m, null, new DateOnly(2021, 9, 1), "UTC-2021", 10, 5, 3, 5.0m, "Quy chế đào tạo UTC 2021 (K64, K65 và các khóa cũ)", 0.30m },
                    { 2L, "Quy chế mới của trường theo Thông tư 56/2026", 5.5m, 1.90m, 2.00m, true, 2.0m, null, new DateOnly(2026, 9, 1), "UTC-2026", 10, 5, 3, 10.0m, "Quy chế đào tạo UTC 2026 (K67 trở đi)", 0.30m }
                });

            migrationBuilder.InsertData(
                table: "VaiTro",
                columns: new[] { "Id", "MaVaiTro", "MoTa", "TenVaiTro" },
                values: new object[,]
                {
                    { 1L, "QuanTri", "Quản lý tài khoản, phân quyền, cấu hình hệ thống.", "Quản trị hệ thống" },
                    { 2L, "PhongDaoTao", "Mở đợt, xét điều kiện, ghi nhận quyết định, chốt và sửa điểm.", "Phòng Đào tạo" },
                    { 3L, "LanhDaoKhoa", "Duyệt đề tài, đề xuất Hội đồng trong phạm vi khoa.", "Lãnh đạo Khoa" },
                    { 4L, "TruongBoMon", "Phân công GVHD/GVĐD, duyệt ngoại lệ quota, tham số bộ môn.", "Trưởng Bộ môn" },
                    { 5L, "GiaoVu", "Xếp lịch bảo vệ, hỗ trợ nghiệp vụ khoa/bộ môn.", "Giáo vụ" },
                    { 6L, "GiangVien", "Vai trò trên từng SV xác định qua PhanCong/ThanhVienHoiDong.", "Giảng viên" },
                    { 7L, "SinhVien", "Đăng ký đề tài, nộp tiến độ, nộp quyển, gửi đơn.", "Sinh viên" },
                    { 8L, "ThuVien", "Tiếp nhận, lưu trữ bản cuối ĐATN.", "Thư viện" }
                });

            migrationBuilder.InsertData(
                table: "RuleSetKhoaHoc",
                columns: new[] { "Id", "CanXacNhan", "KhoaHocDen", "KhoaHocTu", "RuleSetId" },
                values: new object[,]
                {
                    { 1L, false, 65, 1, 1L },
                    { 2L, true, 66, 66, 1L },
                    { 3L, false, null, 67, 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CoSoId_ThoiDiem",
                table: "AuditLog",
                columns: new[] { "CoSoId", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_LoaiDoiTuong_DoiTuongId",
                table: "AuditLog",
                columns: new[] { "LoaiDoiTuong", "DoiTuongId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_NguoiDungId",
                table: "AuditLog",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiem_HocPhanId",
                table: "BangDiem",
                column: "HocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiem_SinhVienId_HocPhanId_HocKy_LanHoc",
                table: "BangDiem",
                columns: new[] { "SinhVienId", "HocPhanId", "HocKy", "LanHoc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTienDo_MocTienDoId",
                table: "BaoCaoTienDo",
                column: "MocTienDoId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTienDo_SVTrongDotId",
                table: "BaoCaoTienDo",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_BienBan_LichBaoVeId",
                table: "BienBan",
                column: "LichBaoVeId");

            migrationBuilder.CreateIndex(
                name: "IX_BienBan_ThuKyId",
                table: "BienBan",
                column: "ThuKyId");

            migrationBuilder.CreateIndex(
                name: "IX_BoMon_KhoaId_MaBM",
                table: "BoMon",
                columns: new[] { "KhoaId", "MaBM" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTao_KhoaId",
                table: "ChuongTrinhDaoTao",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTao_MaCTDT",
                table: "ChuongTrinhDaoTao",
                column: "MaCTDT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoSo_MaCoSo",
                table: "CoSo",
                column: "MaCoSo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CTDTHocPhan_CTDTId_HocPhanId",
                table: "CTDTHocPhan",
                columns: new[] { "CTDTId", "HocPhanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CTDTHocPhan_HocPhanId",
                table: "CTDTHocPhan",
                column: "HocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyNguyenVong_DeTaiId",
                table: "DangKyNguyenVong",
                column: "DeTaiId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyNguyenVong_SVTrongDotId_DeTaiId",
                table: "DangKyNguyenVong",
                columns: new[] { "SVTrongDotId", "DeTaiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DangKyNguyenVong_SVTrongDotId_ThuTuNguyenVong",
                table: "DangKyNguyenVong",
                columns: new[] { "SVTrongDotId", "ThuTuNguyenVong" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_BoMonId",
                table: "DeTai",
                column: "BoMonId");

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_DotId_TrangThaiDuyet",
                table: "DeTai",
                columns: new[] { "DotId", "TrangThaiDuyet" });

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_GVDeXuatId",
                table: "DeTai",
                column: "GVDeXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_NguoiDuyetId",
                table: "DeTai",
                column: "NguoiDuyetId");

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_SVDeXuatId",
                table: "DeTai",
                column: "SVDeXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_ChotBoiId",
                table: "DiemDATN",
                column: "ChotBoiId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_QuyetDinhSuaDiemId",
                table: "DiemDATN",
                column: "QuyetDinhSuaDiemId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_RuleSetId",
                table: "DiemDATN",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_SVTrongDotId",
                table: "DiemDATN",
                column: "SVTrongDotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_NguoiXuLyId",
                table: "DonTu",
                column: "NguoiXuLyId");

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_QuyetDinhId",
                table: "DonTu",
                column: "QuyetDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_SVTrongDotId",
                table: "DonTu",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_TrangThai_LoaiDon",
                table: "DonTu",
                columns: new[] { "TrangThai", "LoaiDon" });

            migrationBuilder.CreateIndex(
                name: "IX_DotDATN_CoSoId_TrangThai",
                table: "DotDATN",
                columns: new[] { "CoSoId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_DotDATN_MaDot",
                table: "DotDATN",
                column: "MaDot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiangVien_BoMonId",
                table: "GiangVien",
                column: "BoMonId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangVien_CoSoId",
                table: "GiangVien",
                column: "CoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangVien_MaGV",
                table: "GiangVien",
                column: "MaGV",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HocPhan_MaHP",
                table: "HocPhan",
                column: "MaHP",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_CoSoId_TrangThai",
                table: "HoiDong",
                columns: new[] { "CoSoId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_DotId_MaHD",
                table: "HoiDong",
                columns: new[] { "DotId", "MaHD" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_KhoaId",
                table: "HoiDong",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_QuyetDinhId",
                table: "HoiDong",
                column: "QuyetDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDocDuyet_GVDDId",
                table: "KetQuaDocDuyet",
                column: "GVDDId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDocDuyet_PhienBanQuyenId",
                table: "KetQuaDocDuyet",
                column: "PhienBanQuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDocDuyet_SVTrongDotId",
                table: "KetQuaDocDuyet",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaKiemTraDaoVan_PhienBanQuyenId",
                table: "KetQuaKiemTraDaoVan",
                column: "PhienBanQuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_Khoa_CoSoId_MaKhoa",
                table: "Khoa",
                columns: new[] { "CoSoId", "MaKhoa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KyLuatSV_SinhVienId",
                table: "KyLuatSV",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoVe_HoiDongId",
                table: "LichBaoVe",
                column: "HoiDongId");

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoVe_PhongBaoVeId_NgayBaoVe_Ca",
                table: "LichBaoVe",
                columns: new[] { "PhongBaoVeId", "NgayBaoVe", "Ca" });

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoVe_SVTrongDotId",
                table: "LichBaoVe",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_DaGui_HanNgay",
                table: "LichNhacViec",
                columns: new[] { "DaGui", "HanNgay" });

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_SVTrongDotId",
                table: "LichNhacViec",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTrangThai_NguoiThucHienId",
                table: "LichSuTrangThai",
                column: "NguoiThucHienId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTrangThai_QuyetDinhId",
                table: "LichSuTrangThai",
                column: "QuyetDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTrangThai_SVTrongDotId_ThoiDiem",
                table: "LichSuTrangThai",
                columns: new[] { "SVTrongDotId", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruThuVien_NguoiNhanId",
                table: "LuuTruThuVien",
                column: "NguoiNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruThuVien_PhienBanQuyenId",
                table: "LuuTruThuVien",
                column: "PhienBanQuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_MocTienDo_DotId_MaMoc",
                table: "MocTienDo",
                columns: new[] { "DotId", "MaMoc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_GiangVienId",
                table: "PhanCong",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_QuyetDinhId",
                table: "PhanCong",
                column: "QuyetDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_SVTrongDotId_VaiTro",
                table: "PhanCong",
                columns: new[] { "SVTrongDotId", "VaiTro" });

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_VuotQuotaDuyetBoiId",
                table: "PhanCong",
                column: "VuotQuotaDuyetBoiId");

            migrationBuilder.CreateIndex(
                name: "IX_PhienBanQuyen_HashSHA256",
                table: "PhienBanQuyen",
                column: "HashSHA256");

            migrationBuilder.CreateIndex(
                name: "IX_PhienBanQuyen_SVTrongDotId_SoPhienBan",
                table: "PhienBanQuyen",
                columns: new[] { "SVTrongDotId", "SoPhienBan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiem_LichBaoVeId_ThanhVienHoiDongId",
                table: "PhieuDiem",
                columns: new[] { "LichBaoVeId", "ThanhVienHoiDongId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiem_NguoiNhapId",
                table: "PhieuDiem",
                column: "NguoiNhapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiem_ThanhVienHoiDongId",
                table: "PhieuDiem",
                column: "ThanhVienHoiDongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuGiaoNhiemVu_SVTrongDotId",
                table: "PhieuGiaoNhiemVu",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_PhongBaoVe_CoSoId_TenPhong",
                table: "PhongBaoVe",
                columns: new[] { "CoSoId", "TenPhong" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinh_CoSoId_SoQuyetDinh",
                table: "QuyetDinh",
                columns: new[] { "CoSoId", "SoQuyetDinh" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_DiemDATNId",
                table: "QuyetDinhDoiTuong",
                column: "DiemDATNId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_HoiDongId",
                table: "QuyetDinhDoiTuong",
                column: "HoiDongId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_PhanCongId",
                table: "QuyetDinhDoiTuong",
                column: "PhanCongId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_QuyetDinhId",
                table: "QuyetDinhDoiTuong",
                column: "QuyetDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_SVTrongDotId",
                table: "QuyetDinhDoiTuong",
                column: "SVTrongDotId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_MaRuleSet",
                table: "RuleSet",
                column: "MaRuleSet",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetKhoaHoc_KhoaHocTu_KhoaHocDen",
                table: "RuleSetKhoaHoc",
                columns: new[] { "KhoaHocTu", "KhoaHocDen" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetKhoaHoc_RuleSetId",
                table: "RuleSetKhoaHoc",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_CoSoId",
                table: "SinhVien",
                column: "CoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_CTDTId",
                table: "SinhVien",
                column: "CTDTId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_KhoaHoc",
                table: "SinhVien",
                column: "KhoaHoc");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_MaSV",
                table: "SinhVien",
                column: "MaSV",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_CoSoId_TrangThai",
                table: "SVTrongDot",
                columns: new[] { "CoSoId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_CTDTId",
                table: "SVTrongDot",
                column: "CTDTId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_DeTaiId",
                table: "SVTrongDot",
                column: "DeTaiId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_DotId_SinhVienId_CTDTId",
                table: "SVTrongDot",
                columns: new[] { "DotId", "SinhVienId", "CTDTId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_DotId_TrangThai",
                table: "SVTrongDot",
                columns: new[] { "DotId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_HoSoTruocId",
                table: "SVTrongDot",
                column: "HoSoTruocId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_QuyetDinhGiaoId",
                table: "SVTrongDot",
                column: "QuyetDinhGiaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_SinhVienId",
                table: "SVTrongDot",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_GiangVienId",
                table: "TaiKhoan",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_SinhVienId",
                table: "TaiKhoan",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_BoMonId",
                table: "TaiKhoanVaiTro",
                column: "BoMonId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_CoSoId",
                table: "TaiKhoanVaiTro",
                column: "CoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_KhoaId",
                table: "TaiKhoanVaiTro",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_TaiKhoanId",
                table: "TaiKhoanVaiTro",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_VaiTroId",
                table: "TaiKhoanVaiTro",
                column: "VaiTroId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoBoMon_BoMonId_RuleSetId",
                table: "ThamSoBoMon",
                columns: new[] { "BoMonId", "RuleSetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoBoMon_RuleSetId",
                table: "ThamSoBoMon",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoDaoVan_KhoaId_ChuongTrinh",
                table: "ThamSoDaoVan",
                columns: new[] { "KhoaId", "ChuongTrinh" });

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoDaoVan_RuleSetId",
                table: "ThamSoDaoVan",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienHoiDong_GiangVienId",
                table: "ThanhVienHoiDong",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienHoiDong_HoiDongId",
                table: "ThanhVienHoiDong",
                column: "HoiDongId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienHoiDong_ThayTheChoId",
                table: "ThanhVienHoiDong",
                column: "ThayTheChoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinTienDoSV_SinhVienId",
                table: "ThongTinTienDoSV",
                column: "SinhVienId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_MaVaiTro",
                table: "VaiTro",
                column: "MaVaiTro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKien_NguoiChayId",
                table: "XetDieuKien",
                column: "NguoiChayId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKien_RuleSetId",
                table: "XetDieuKien",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKien_SVTrongDotId_LoaiXet_ThoiDiemXet",
                table: "XetDieuKien",
                columns: new[] { "SVTrongDotId", "LoaiXet", "ThoiDiemXet" });

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKienChiTiet_HocPhanId",
                table: "XetDieuKienChiTiet",
                column: "HocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKienChiTiet_XetDieuKienId",
                table: "XetDieuKienChiTiet",
                column: "XetDieuKienId");

            migrationBuilder.CreateIndex(
                name: "IX_YKienGVHD_GiangVienId",
                table: "YKienGVHD",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_YKienGVHD_SVTrongDotId",
                table: "YKienGVHD",
                column: "SVTrongDotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BangDiem");

            migrationBuilder.DropTable(
                name: "BaoCaoTienDo");

            migrationBuilder.DropTable(
                name: "BienBan");

            migrationBuilder.DropTable(
                name: "CTDTHocPhan");

            migrationBuilder.DropTable(
                name: "DangKyNguyenVong");

            migrationBuilder.DropTable(
                name: "DonTu");

            migrationBuilder.DropTable(
                name: "KetQuaDocDuyet");

            migrationBuilder.DropTable(
                name: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropTable(
                name: "KyLuatSV");

            migrationBuilder.DropTable(
                name: "LichNhacViec");

            migrationBuilder.DropTable(
                name: "LichSuTrangThai");

            migrationBuilder.DropTable(
                name: "LuuTruThuVien");

            migrationBuilder.DropTable(
                name: "PhieuDiem");

            migrationBuilder.DropTable(
                name: "PhieuGiaoNhiemVu");

            migrationBuilder.DropTable(
                name: "QuyetDinhDoiTuong");

            migrationBuilder.DropTable(
                name: "RuleSetKhoaHoc");

            migrationBuilder.DropTable(
                name: "TaiKhoanVaiTro");

            migrationBuilder.DropTable(
                name: "ThamSoBoMon");

            migrationBuilder.DropTable(
                name: "ThamSoDaoVan");

            migrationBuilder.DropTable(
                name: "ThongTinTienDoSV");

            migrationBuilder.DropTable(
                name: "XetDieuKienChiTiet");

            migrationBuilder.DropTable(
                name: "YKienGVHD");

            migrationBuilder.DropTable(
                name: "MocTienDo");

            migrationBuilder.DropTable(
                name: "PhienBanQuyen");

            migrationBuilder.DropTable(
                name: "LichBaoVe");

            migrationBuilder.DropTable(
                name: "ThanhVienHoiDong");

            migrationBuilder.DropTable(
                name: "DiemDATN");

            migrationBuilder.DropTable(
                name: "PhanCong");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "HocPhan");

            migrationBuilder.DropTable(
                name: "XetDieuKien");

            migrationBuilder.DropTable(
                name: "PhongBaoVe");

            migrationBuilder.DropTable(
                name: "HoiDong");

            migrationBuilder.DropTable(
                name: "RuleSet");

            migrationBuilder.DropTable(
                name: "SVTrongDot");

            migrationBuilder.DropTable(
                name: "DeTai");

            migrationBuilder.DropTable(
                name: "QuyetDinh");

            migrationBuilder.DropTable(
                name: "DotDATN");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "GiangVien");

            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "BoMon");

            migrationBuilder.DropTable(
                name: "ChuongTrinhDaoTao");

            migrationBuilder.DropTable(
                name: "Khoa");

            migrationBuilder.DropTable(
                name: "CoSo");
        }
    }
}
