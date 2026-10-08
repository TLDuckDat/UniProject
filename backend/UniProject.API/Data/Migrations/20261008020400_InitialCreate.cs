using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UniProject.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HOCHAM",
                columns: table => new
                {
                    MaHocHam = table.Column<int>(type: "int", nullable: false),
                    TenHocHam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOCHAM", x => x.MaHocHam);
                });

            migrationBuilder.CreateTable(
                name: "HOCVI",
                columns: table => new
                {
                    MaHocVi = table.Column<int>(type: "int", nullable: false),
                    TenHocVi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOCVI", x => x.MaHocVi);
                });

            migrationBuilder.CreateTable(
                name: "KHOA",
                columns: table => new
                {
                    MaKhoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhoaCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KHOA", x => x.MaKhoa);
                });

            migrationBuilder.CreateTable(
                name: "NAMHOC",
                columns: table => new
                {
                    MaNamHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNamHoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NAMHOC", x => x.MaNamHoc);
                    table.CheckConstraint("CK_NAMHOC_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
                });

            migrationBuilder.CreateTable(
                name: "VAITRO",
                columns: table => new
                {
                    MaVaiTro = table.Column<int>(type: "int", nullable: false),
                    TenVaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VAITRO", x => x.MaVaiTro);
                });

            migrationBuilder.CreateTable(
                name: "BOMON",
                columns: table => new
                {
                    MaBoMon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBoMonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenBoMon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaKhoa = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMON", x => x.MaBoMon);
                    table.ForeignKey(
                        name: "FK_BOMON_KHOA",
                        column: x => x.MaKhoa,
                        principalTable: "KHOA",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HOCKY",
                columns: table => new
                {
                    MaHocKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenHocKy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HocKySo = table.Column<int>(type: "int", nullable: false),
                    MaNamHoc = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOCKY", x => x.MaHocKy);
                    table.CheckConstraint("CK_HOCKY_HocKySo", "[HocKySo] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_HOCKY_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
                    table.ForeignKey(
                        name: "FK_HOCKY_NAMHOC",
                        column: x => x.MaNamHoc,
                        principalTable: "NAMHOC",
                        principalColumn: "MaNamHoc",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TAIKHOAN",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaVaiTro = table.Column<int>(type: "int", nullable: false),
                    DaKhoa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LanDangNhapCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NguoiTao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAIKHOAN", x => x.MaTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TAIKHOAN_VAITRO",
                        column: x => x.MaVaiTro,
                        principalTable: "VAITRO",
                        principalColumn: "MaVaiTro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHUYENNGANH",
                columns: table => new
                {
                    MaChuyenNganh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaChuyenNganhCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenChuyenNganh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaBoMon = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHUYENNGANH", x => x.MaChuyenNganh);
                    table.ForeignKey(
                        name: "FK_CHUYENNGANH_BOMON",
                        column: x => x.MaBoMon,
                        principalTable: "BOMON",
                        principalColumn: "MaBoMon",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOTDOAN",
                columns: table => new
                {
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDotDoAnCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenDotDoAn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LoaiDoAn = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaHocKy = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanDangKyDeTai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanNopDeCuong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanNopBaoCao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayBatDauBaoVe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThucBaoVe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOTDOAN", x => x.MaDotDoAn);
                    table.CheckConstraint("CK_DOTDOAN_NgayBaoVe", "[NgayKetThucBaoVe] >= [NgayBatDauBaoVe]");
                    table.CheckConstraint("CK_DOTDOAN_NgayThucHien", "[NgayKetThuc] >= [NgayBatDau]");
                    table.ForeignKey(
                        name: "FK_DOTDOAN_HOCKY_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HOCKY",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GIANGVIEN",
                columns: table => new
                {
                    MaGV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGVCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaBoMon = table.Column<int>(type: "int", nullable: false),
                    MaHocVi = table.Column<int>(type: "int", nullable: false),
                    MaHocHam = table.Column<int>(type: "int", nullable: true),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GIANGVIEN", x => x.MaGV);
                    table.ForeignKey(
                        name: "FK_GIANGVIEN_BOMON",
                        column: x => x.MaBoMon,
                        principalTable: "BOMON",
                        principalColumn: "MaBoMon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GIANGVIEN_HOCHAM",
                        column: x => x.MaHocHam,
                        principalTable: "HOCHAM",
                        principalColumn: "MaHocHam",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GIANGVIEN_HOCVI",
                        column: x => x.MaHocVi,
                        principalTable: "HOCVI",
                        principalColumn: "MaHocVi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GIANGVIEN_TAIKHOAN_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "THONGBAO",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MucDo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NguoiTaoId = table.Column<int>(type: "int", nullable: true),
                    NgayTaoRecord = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THONGBAO", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_THONGBAO_TAIKHOAN_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LOP",
                columns: table => new
                {
                    MaLop = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLopCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenLop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NamNhapHoc = table.Column<int>(type: "int", nullable: false),
                    MaChuyenNganh = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOP", x => x.MaLop);
                    table.CheckConstraint("CK_LOP_NamNhapHoc", "[NamNhapHoc] BETWEEN 2000 AND 2100");
                    table.ForeignKey(
                        name: "FK_LOP_CHUYENNGANH",
                        column: x => x.MaChuyenNganh,
                        principalTable: "CHUYENNGANH",
                        principalColumn: "MaChuyenNganh",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DANGKYDETAI",
                columns: table => new
                {
                    MaDangKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    TenNhom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DANGKYDETAI", x => x.MaDangKy);
                    table.ForeignKey(
                        name: "FK_DANGKYDETAI_DOTDOAN_MaDotDoAn",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HOIDONG",
                columns: table => new
                {
                    MaHoiDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiDongCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenHoiDong = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    DiaDiem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTaoRecord = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOIDONG", x => x.MaHoiDong);
                    table.ForeignKey(
                        name: "FK_HOIDONG_DOTDOAN_MaDotDoAn",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NGUOINHAN_THONGBAO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThongBao = table.Column<int>(type: "int", nullable: false),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NgayDoc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NGUOINHAN_THONGBAO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NGUOINHANTHONGBAO_THONGBAO",
                        column: x => x.MaThongBao,
                        principalTable: "THONGBAO",
                        principalColumn: "MaThongBao",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NGUOINHAN_THONGBAO_TAIKHOAN_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOTDOAN_LOP",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    MaLop = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOTDOAN_LOP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DOTDOANLOP_DOTDOAN",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOTDOAN_LOP_LOP_MaLop",
                        column: x => x.MaLop,
                        principalTable: "LOP",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SINHVIEN",
                columns: table => new
                {
                    MaSV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSVCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaLop = table.Column<int>(type: "int", nullable: false),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SINHVIEN", x => x.MaSV);
                    table.ForeignKey(
                        name: "FK_SINHVIEN_LOP",
                        column: x => x.MaLop,
                        principalTable: "LOP",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SINHVIEN_TAIKHOAN_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "THANHVIEN_HOIDONG",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiDong = table.Column<int>(type: "int", nullable: false),
                    MaGV = table.Column<int>(type: "int", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THANHVIEN_HOIDONG", x => x.Id);
                    table.ForeignKey(
                        name: "FK_THANHVIENHOIDONG_HOIDONG",
                        column: x => x.MaHoiDong,
                        principalTable: "HOIDONG",
                        principalColumn: "MaHoiDong",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_THANHVIEN_HOIDONG_GIANGVIEN_MaGV",
                        column: x => x.MaGV,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DETAI",
                columns: table => new
                {
                    MaDeTai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDeTaiCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenDeTai = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MucTieu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YeuCau = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CongNgheDuKien = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SoLuongSinhVienToiDa = table.Column<int>(type: "int", nullable: false),
                    MaGVDeXuat = table.Column<int>(type: "int", nullable: false),
                    MaSVDeXuat = table.Column<int>(type: "int", nullable: true),
                    MaChuyenNganh = table.Column<int>(type: "int", nullable: false),
                    LoaiDoAn = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaGVDuyet = table.Column<int>(type: "int", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETAI", x => x.MaDeTai);
                    table.CheckConstraint("CK_DETAI_SoLuongSinhVien", "[SoLuongSinhVienToiDa] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_DETAI_CHUYENNGANH_MaChuyenNganh",
                        column: x => x.MaChuyenNganh,
                        principalTable: "CHUYENNGANH",
                        principalColumn: "MaChuyenNganh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DETAI_GIANGVIEN_MaGVDeXuat",
                        column: x => x.MaGVDeXuat,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DETAI_GIANGVIEN_MaGVDuyet",
                        column: x => x.MaGVDuyet,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DETAI_SINHVIEN_MaSVDeXuat",
                        column: x => x.MaSVDeXuat,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "THANHVIEN_DANGKY",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayThamGia = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THANHVIEN_DANGKY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_THANHVIENDANGKY_DANGKYDETAI",
                        column: x => x.MaDangKy,
                        principalTable: "DANGKYDETAI",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_THANHVIEN_DANGKY_SINHVIEN_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DETAI_DOTDOAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDeTai = table.Column<int>(type: "int", nullable: false),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETAI_DOTDOAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DETAIDOTDOAN_DETAI",
                        column: x => x.MaDeTai,
                        principalTable: "DETAI",
                        principalColumn: "MaDeTai",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DETAI_DOTDOAN_DOTDOAN_MaDotDoAn",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOAN",
                columns: table => new
                {
                    MaDoAn = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAnCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenDoAn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    MaDeTai = table.Column<int>(type: "int", nullable: false),
                    MaDangKy = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTaoRecord = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOAN", x => x.MaDoAn);
                    table.ForeignKey(
                        name: "FK_DOAN_DANGKYDETAI_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DANGKYDETAI",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOAN_DETAI_MaDeTai",
                        column: x => x.MaDeTai,
                        principalTable: "DETAI",
                        principalColumn: "MaDeTai",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOAN_DOTDOAN_MaDotDoAn",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHITIET_DANGKY",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaDeTaiDotDoAn = table.Column<int>(type: "int", nullable: false),
                    ThuTuUuTien = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaGVDuyet = table.Column<int>(type: "int", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHITIET_DANGKY", x => x.Id);
                    table.CheckConstraint("CK_CHITIETDANGKY_ThuTu", "[ThuTuUuTien] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_CHITIETDANGKY_DANGKYDETAI",
                        column: x => x.MaDangKy,
                        principalTable: "DANGKYDETAI",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CHITIET_DANGKY_DETAI_DOTDOAN_MaDeTaiDotDoAn",
                        column: x => x.MaDeTaiDotDoAn,
                        principalTable: "DETAI_DOTDOAN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHITIET_DANGKY_GIANGVIEN_MaGVDuyet",
                        column: x => x.MaGVDuyet,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YEUCAU_THAYDOI",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaSVYeuCau = table.Column<int>(type: "int", nullable: false),
                    LoaiYeuCau = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaDeTaiDotDoAnMoi = table.Column<int>(type: "int", nullable: true),
                    MaDangKyMoi = table.Column<int>(type: "int", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MaTaiKhoanXuLy = table.Column<int>(type: "int", nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChuXuLy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YEUCAU_THAYDOI", x => x.MaYeuCau);
                    table.CheckConstraint("CK_YEUCAUTHAYDOI_Loai", "([LoaiYeuCau] = 'DOI_DE_TAI' AND [MaDeTaiDotDoAnMoi] IS NOT NULL AND [MaDangKyMoi] IS NULL) OR ([LoaiYeuCau] = 'DOI_NHOM' AND [MaDangKyMoi] IS NOT NULL AND [MaDeTaiDotDoAnMoi] IS NULL)");
                    table.ForeignKey(
                        name: "FK_YEUCAUTHAYDOI_DANGKYDETAI",
                        column: x => x.MaDangKy,
                        principalTable: "DANGKYDETAI",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YEUCAUTHAYDOI_DANGKYDETAI_Moi",
                        column: x => x.MaDangKyMoi,
                        principalTable: "DANGKYDETAI",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YEUCAU_THAYDOI_DETAI_DOTDOAN_MaDeTaiDotDoAnMoi",
                        column: x => x.MaDeTaiDotDoAnMoi,
                        principalTable: "DETAI_DOTDOAN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YEUCAU_THAYDOI_SINHVIEN_MaSVYeuCau",
                        column: x => x.MaSVYeuCau,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YEUCAU_THAYDOI_TAIKHOAN_MaTaiKhoanXuLy",
                        column: x => x.MaTaiKhoanXuLy,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BAOCAO_TIENDO",
                columns: table => new
                {
                    MaBaoCao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaSVNop = table.Column<int>(type: "int", nullable: false),
                    LanBaoCao = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CongViecDaLam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CongViecTiepTheo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KhoKhan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhanTramHoanThanh = table.Column<int>(type: "int", nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAOCAO_TIENDO", x => x.MaBaoCao);
                    table.CheckConstraint("CK_BAOCAOTIENDO_PhanTram", "[PhanTramHoanThanh] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_BAOCAOTIENDO_DOAN",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BAOCAO_TIENDO_SINHVIEN_MaSVNop",
                        column: x => x.MaSVNop,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KETQUA_DOAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    DiemHuongDan = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    DiemPhanBien = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    DiemHoiDong = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    DiemTongKet = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    XepLoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayCongBo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KETQUA_DOAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KETQUA_DOAN_DOAN_MaDoAn",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KETQUA_DOAN_SINHVIEN_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LICH_BAOVE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaHoiDong = table.Column<int>(type: "int", nullable: false),
                    MaDotDoAn = table.Column<int>(type: "int", nullable: false),
                    NgayBaoVe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    Phong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICH_BAOVE", x => x.Id);
                    table.CheckConstraint("CK_LICHBAOVE_Gio", "[GioKetThuc] > [GioBatDau]");
                    table.ForeignKey(
                        name: "FK_LICHBAOVE_HOIDONG",
                        column: x => x.MaHoiDong,
                        principalTable: "HOIDONG",
                        principalColumn: "MaHoiDong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LICH_BAOVE_DOAN_MaDoAn",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LICH_BAOVE_DOTDOAN_MaDotDoAn",
                        column: x => x.MaDotDoAn,
                        principalTable: "DOTDOAN",
                        principalColumn: "MaDotDoAn",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHANBIEN_DOAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaGV = table.Column<int>(type: "int", nullable: false),
                    NgayPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHANBIEN_DOAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PHANBIEN_DOAN_DOAN_MaDoAn",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHANBIEN_DOAN_GIANGVIEN_MaGV",
                        column: x => x.MaGV,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHANCONG_HUONGDAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaGV = table.Column<int>(type: "int", nullable: false),
                    LoaiHuongDan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHANCONG_HUONGDAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PHANCONGHUONGDAN_DOAN",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PHANCONG_HUONGDAN_GIANGVIEN_MaGV",
                        column: x => x.MaGV,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHIEUCHAM",
                columns: table => new
                {
                    MaPhieuCham = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    MaGV = table.Column<int>(type: "int", nullable: false),
                    MaHoiDong = table.Column<int>(type: "int", nullable: true),
                    LoaiCham = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Diem = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayCham = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEUCHAM", x => x.MaPhieuCham);
                    table.CheckConstraint("CK_PHIEUCHAM_Diem", "[Diem] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_PHIEUCHAM_DOAN_MaDoAn",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHIEUCHAM_GIANGVIEN_MaGV",
                        column: x => x.MaGV,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHIEUCHAM_HOIDONG_MaHoiDong",
                        column: x => x.MaHoiDong,
                        principalTable: "HOIDONG",
                        principalColumn: "MaHoiDong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHIEUCHAM_SINHVIEN_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TAILIEU_DOAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaSVNop = table.Column<int>(type: "int", nullable: false),
                    TenTaiLieu = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DuongDanFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoaiTaiLieu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    NgayUpload = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAILIEU_DOAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TAILIEUDOAN_DOAN",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TAILIEU_DOAN_SINHVIEN_MaSVNop",
                        column: x => x.MaSVNop,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "THANHVIEN_DOAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THANHVIEN_DOAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_THANHVIENDOAN_DOAN",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_THANHVIEN_DOAN_SINHVIEN_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SINHVIEN",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YEUCAU_CHINHSUA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDoAn = table.Column<int>(type: "int", nullable: false),
                    MaGVYeuCau = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HanHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YEUCAU_CHINHSUA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YEUCAUCHINHSUA_DOAN",
                        column: x => x.MaDoAn,
                        principalTable: "DOAN",
                        principalColumn: "MaDoAn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YEUCAU_CHINHSUA_GIANGVIEN_MaGVYeuCau",
                        column: x => x.MaGVYeuCau,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NHANXET_TIENDO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBaoCao = table.Column<int>(type: "int", nullable: false),
                    MaGV = table.Column<int>(type: "int", nullable: false),
                    NoiDungNhanXet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MucDanhGia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayNhanXet = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NHANXET_TIENDO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NHANXETTIENDO_BAOCAOTIENDO",
                        column: x => x.MaBaoCao,
                        principalTable: "BAOCAO_TIENDO",
                        principalColumn: "MaBaoCao",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NHANXET_TIENDO_GIANGVIEN_MaGV",
                        column: x => x.MaGV,
                        principalTable: "GIANGVIEN",
                        principalColumn: "MaGV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HOCHAM",
                columns: new[] { "MaHocHam", "TenHocHam" },
                values: new object[,]
                {
                    { 1, "Phó Giáo sư" },
                    { 2, "Giáo sư" }
                });

            migrationBuilder.InsertData(
                table: "HOCVI",
                columns: new[] { "MaHocVi", "TenHocVi" },
                values: new object[,]
                {
                    { 1, "Cử nhân" },
                    { 2, "Kỹ sư" },
                    { 3, "Thạc sĩ" },
                    { 4, "Tiến sĩ" }
                });

            migrationBuilder.InsertData(
                table: "VAITRO",
                columns: new[] { "MaVaiTro", "MoTa", "TenVaiTro" },
                values: new object[,]
                {
                    { 1, "Quản trị hệ thống", "ADMIN" },
                    { 2, "Giảng viên hướng dẫn / phản biện / hội đồng", "GIANG_VIEN" },
                    { 3, "Sinh viên thực hiện đồ án", "SINH_VIEN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BAOCAO_TIENDO_MaSVNop",
                table: "BAOCAO_TIENDO",
                column: "MaSVNop");

            migrationBuilder.CreateIndex(
                name: "IX_BAOCAOTIENDO_MaDoAn",
                table: "BAOCAO_TIENDO",
                column: "MaDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_BOMON_MaKhoa",
                table: "BOMON",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "UQ_BOMON_MaBoMonCode",
                table: "BOMON",
                column: "MaBoMonCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_BOMON_TenBoMon_MaKhoa",
                table: "BOMON",
                columns: new[] { "TenBoMon", "MaKhoa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHITIET_DANGKY_MaDeTaiDotDoAn",
                table: "CHITIET_DANGKY",
                column: "MaDeTaiDotDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_CHITIET_DANGKY_MaGVDuyet",
                table: "CHITIET_DANGKY",
                column: "MaGVDuyet");

            migrationBuilder.CreateIndex(
                name: "UQ_CHITIETDANGKY_MaDangKy_MaDeTaiDotDoAn",
                table: "CHITIET_DANGKY",
                columns: new[] { "MaDangKy", "MaDeTaiDotDoAn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CHITIETDANGKY_MaDangKy_ThuTuUuTien",
                table: "CHITIET_DANGKY",
                columns: new[] { "MaDangKy", "ThuTuUuTien" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CHITIETDANGKY_OneApprovedPreference",
                table: "CHITIET_DANGKY",
                column: "MaDangKy",
                unique: true,
                filter: "[TrangThai] = 'DUOC_CHON'");

            migrationBuilder.CreateIndex(
                name: "IX_CHUYENNGANH_MaBoMon",
                table: "CHUYENNGANH",
                column: "MaBoMon");

            migrationBuilder.CreateIndex(
                name: "UQ_CHUYENNGANH_MaChuyenNganhCode",
                table: "CHUYENNGANH",
                column: "MaChuyenNganhCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CHUYENNGANH_TenChuyenNganh_MaBoMon",
                table: "CHUYENNGANH",
                columns: new[] { "TenChuyenNganh", "MaBoMon" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DANGKYDETAI_MaDotDoAn",
                table: "DANGKYDETAI",
                column: "MaDotDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_DETAI_MaChuyenNganh",
                table: "DETAI",
                column: "MaChuyenNganh");

            migrationBuilder.CreateIndex(
                name: "IX_DETAI_MaGVDeXuat",
                table: "DETAI",
                column: "MaGVDeXuat");

            migrationBuilder.CreateIndex(
                name: "IX_DETAI_MaGVDuyet",
                table: "DETAI",
                column: "MaGVDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_DETAI_MaSVDeXuat",
                table: "DETAI",
                column: "MaSVDeXuat");

            migrationBuilder.CreateIndex(
                name: "UQ_DETAI_MaDeTaiCode",
                table: "DETAI",
                column: "MaDeTaiCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DETAIDOTDOAN_MaDotDoAn",
                table: "DETAI_DOTDOAN",
                column: "MaDotDoAn");

            migrationBuilder.CreateIndex(
                name: "UQ_DETAIDOTDOAN_MaDeTai_MaDotDoAn",
                table: "DETAI_DOTDOAN",
                columns: new[] { "MaDeTai", "MaDotDoAn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOAN_MaDangKy",
                table: "DOAN",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_DOAN_MaDeTai",
                table: "DOAN",
                column: "MaDeTai");

            migrationBuilder.CreateIndex(
                name: "IX_DOAN_MaDotDoAn",
                table: "DOAN",
                column: "MaDotDoAn");

            migrationBuilder.CreateIndex(
                name: "UQ_DOAN_MaDoAnCode",
                table: "DOAN",
                column: "MaDoAnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOTDOAN_LoaiDoAn",
                table: "DOTDOAN",
                column: "LoaiDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_DOTDOAN_MaHocKy",
                table: "DOTDOAN",
                column: "MaHocKy");

            migrationBuilder.CreateIndex(
                name: "UQ_DOTDOAN_MaDotDoAnCode",
                table: "DOTDOAN",
                column: "MaDotDoAnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOTDOAN_LOP_MaLop",
                table: "DOTDOAN_LOP",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "UQ_DOTDOANLOP_MaDotDoAn_MaLop",
                table: "DOTDOAN_LOP",
                columns: new[] { "MaDotDoAn", "MaLop" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GIANGVIEN_MaBoMon",
                table: "GIANGVIEN",
                column: "MaBoMon");

            migrationBuilder.CreateIndex(
                name: "IX_GIANGVIEN_MaHocHam",
                table: "GIANGVIEN",
                column: "MaHocHam");

            migrationBuilder.CreateIndex(
                name: "IX_GIANGVIEN_MaHocVi",
                table: "GIANGVIEN",
                column: "MaHocVi");

            migrationBuilder.CreateIndex(
                name: "UQ_GIANGVIEN_Email",
                table: "GIANGVIEN",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_GIANGVIEN_MaGVCode",
                table: "GIANGVIEN",
                column: "MaGVCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_GIANGVIEN_MaTaiKhoan",
                table: "GIANGVIEN",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_HOCHAM_TenHocHam",
                table: "HOCHAM",
                column: "TenHocHam",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HOCKY_MaNamHoc",
                table: "HOCKY",
                column: "MaNamHoc");

            migrationBuilder.CreateIndex(
                name: "UQ_HOCKY_HocKySo_MaNamHoc",
                table: "HOCKY",
                columns: new[] { "HocKySo", "MaNamHoc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_HOCVI_TenHocVi",
                table: "HOCVI",
                column: "TenHocVi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HOIDONG_MaDotDoAn",
                table: "HOIDONG",
                column: "MaDotDoAn");

            migrationBuilder.CreateIndex(
                name: "UQ_HOIDONG_MaHoiDongCode",
                table: "HOIDONG",
                column: "MaHoiDongCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KETQUA_DOAN_MaSV",
                table: "KETQUA_DOAN",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "UQ_KETQUADOAN_MaDoAn_MaSV",
                table: "KETQUA_DOAN",
                columns: new[] { "MaDoAn", "MaSV" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_KHOA_MaKhoaCode",
                table: "KHOA",
                column: "MaKhoaCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_KHOA_TenKhoa",
                table: "KHOA",
                column: "TenKhoa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LICH_BAOVE_MaDoAn",
                table: "LICH_BAOVE",
                column: "MaDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_BAOVE_MaDotDoAn",
                table: "LICH_BAOVE",
                column: "MaDotDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_BAOVE_MaHoiDong",
                table: "LICH_BAOVE",
                column: "MaHoiDong");

            migrationBuilder.CreateIndex(
                name: "IX_LICHBAOVE_CommitteeSchedule",
                table: "LICH_BAOVE",
                columns: new[] { "NgayBaoVe", "MaHoiDong", "GioBatDau", "GioKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_LICHBAOVE_RoomSchedule",
                table: "LICH_BAOVE",
                columns: new[] { "NgayBaoVe", "Phong", "GioBatDau", "GioKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_LOP_MaChuyenNganh",
                table: "LOP",
                column: "MaChuyenNganh");

            migrationBuilder.CreateIndex(
                name: "UQ_LOP_MaLopCode",
                table: "LOP",
                column: "MaLopCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_NAMHOC_TenNamHoc",
                table: "NAMHOC",
                column: "TenNamHoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NGUOINHANTHONGBAO_MaTaiKhoan",
                table: "NGUOINHAN_THONGBAO",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "UQ_NGUOINHANTHONGBAO_ThongBao_TaiKhoan",
                table: "NGUOINHAN_THONGBAO",
                columns: new[] { "MaThongBao", "MaTaiKhoan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NHANXET_TIENDO_MaBaoCao",
                table: "NHANXET_TIENDO",
                column: "MaBaoCao");

            migrationBuilder.CreateIndex(
                name: "IX_NHANXET_TIENDO_MaGV",
                table: "NHANXET_TIENDO",
                column: "MaGV");

            migrationBuilder.CreateIndex(
                name: "IX_PHANBIENDOAN_MaGV",
                table: "PHANBIEN_DOAN",
                column: "MaGV");

            migrationBuilder.CreateIndex(
                name: "UQ_PHANBIENDOAN_MaDoAn_MaGV",
                table: "PHANBIEN_DOAN",
                columns: new[] { "MaDoAn", "MaGV" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PHANCONG_HUONGDAN_MaGV",
                table: "PHANCONG_HUONGDAN",
                column: "MaGV");

            migrationBuilder.CreateIndex(
                name: "UQ_PHANCONGHUONGDAN_OnePrimaryAdvisor",
                table: "PHANCONG_HUONGDAN",
                column: "MaDoAn",
                unique: true,
                filter: "[LoaiHuongDan] = 'CHINH' AND [TrangThai] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEUCHAM_MaDoAn_MaSV",
                table: "PHIEUCHAM",
                columns: new[] { "MaDoAn", "MaSV" });

            migrationBuilder.CreateIndex(
                name: "IX_PHIEUCHAM_MaGV",
                table: "PHIEUCHAM",
                column: "MaGV");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEUCHAM_MaHoiDong",
                table: "PHIEUCHAM",
                column: "MaHoiDong");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEUCHAM_MaSV",
                table: "PHIEUCHAM",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "UQ_PHIEUCHAM_DoAn_SV_GV_LoaiCham",
                table: "PHIEUCHAM",
                columns: new[] { "MaDoAn", "MaSV", "MaGV", "LoaiCham" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SINHVIEN_MaLop",
                table: "SINHVIEN",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "UQ_SINHVIEN_Email",
                table: "SINHVIEN",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SINHVIEN_MaSVCode",
                table: "SINHVIEN",
                column: "MaSVCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SINHVIEN_MaTaiKhoan",
                table: "SINHVIEN",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TAIKHOAN_MaVaiTro",
                table: "TAIKHOAN",
                column: "MaVaiTro");

            migrationBuilder.CreateIndex(
                name: "UQ_TAIKHOAN_Email",
                table: "TAIKHOAN",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TAIKHOAN_TenDangNhap",
                table: "TAIKHOAN",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TAILIEU_DOAN_MaDoAn",
                table: "TAILIEU_DOAN",
                column: "MaDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_TAILIEU_DOAN_MaSVNop",
                table: "TAILIEU_DOAN",
                column: "MaSVNop");

            migrationBuilder.CreateIndex(
                name: "IX_THANHVIEN_DANGKY_MaSV",
                table: "THANHVIEN_DANGKY",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENDANGKY_MaDangKy_MaSV",
                table: "THANHVIEN_DANGKY",
                columns: new[] { "MaDangKy", "MaSV" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENDANGKY_OneLeader",
                table: "THANHVIEN_DANGKY",
                column: "MaDangKy",
                unique: true,
                filter: "[VaiTro] = 'TRUONG_NHOM'");

            migrationBuilder.CreateIndex(
                name: "IX_THANHVIEN_DOAN_MaSV",
                table: "THANHVIEN_DOAN",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENDOAN_MaDoAn_MaSV",
                table: "THANHVIEN_DOAN",
                columns: new[] { "MaDoAn", "MaSV" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENDOAN_OneLeader",
                table: "THANHVIEN_DOAN",
                column: "MaDoAn",
                unique: true,
                filter: "[VaiTro] = 'TRUONG_NHOM'");

            migrationBuilder.CreateIndex(
                name: "IX_THANHVIEN_HOIDONG_MaGV",
                table: "THANHVIEN_HOIDONG",
                column: "MaGV");

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENHOIDONG_MaHoiDong_MaGV",
                table: "THANHVIEN_HOIDONG",
                columns: new[] { "MaHoiDong", "MaGV" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENHOIDONG_OneChuTich",
                table: "THANHVIEN_HOIDONG",
                column: "MaHoiDong",
                unique: true,
                filter: "[VaiTro] = 'CHU_TICH'");

            migrationBuilder.CreateIndex(
                name: "UQ_THANHVIENHOIDONG_OneThuKy",
                table: "THANHVIEN_HOIDONG",
                column: "MaHoiDong",
                unique: true,
                filter: "[VaiTro] = 'THU_KY'");

            migrationBuilder.CreateIndex(
                name: "IX_THONGBAO_NguoiTaoId",
                table: "THONGBAO",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "UQ_VAITRO_TenVaiTro",
                table: "VAITRO",
                column: "TenVaiTro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAU_CHINHSUA_MaDoAn",
                table: "YEUCAU_CHINHSUA",
                column: "MaDoAn");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAU_CHINHSUA_MaGVYeuCau",
                table: "YEUCAU_CHINHSUA",
                column: "MaGVYeuCau");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAU_THAYDOI_MaDangKyMoi",
                table: "YEUCAU_THAYDOI",
                column: "MaDangKyMoi");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAU_THAYDOI_MaDeTaiDotDoAnMoi",
                table: "YEUCAU_THAYDOI",
                column: "MaDeTaiDotDoAnMoi");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAU_THAYDOI_MaTaiKhoanXuLy",
                table: "YEUCAU_THAYDOI",
                column: "MaTaiKhoanXuLy");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAUTHAYDOI_MaDangKy",
                table: "YEUCAU_THAYDOI",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_YEUCAUTHAYDOI_MaSV_TrangThai",
                table: "YEUCAU_THAYDOI",
                columns: new[] { "MaSVYeuCau", "TrangThai" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHITIET_DANGKY");

            migrationBuilder.DropTable(
                name: "DOTDOAN_LOP");

            migrationBuilder.DropTable(
                name: "KETQUA_DOAN");

            migrationBuilder.DropTable(
                name: "LICH_BAOVE");

            migrationBuilder.DropTable(
                name: "NGUOINHAN_THONGBAO");

            migrationBuilder.DropTable(
                name: "NHANXET_TIENDO");

            migrationBuilder.DropTable(
                name: "PHANBIEN_DOAN");

            migrationBuilder.DropTable(
                name: "PHANCONG_HUONGDAN");

            migrationBuilder.DropTable(
                name: "PHIEUCHAM");

            migrationBuilder.DropTable(
                name: "TAILIEU_DOAN");

            migrationBuilder.DropTable(
                name: "THANHVIEN_DANGKY");

            migrationBuilder.DropTable(
                name: "THANHVIEN_DOAN");

            migrationBuilder.DropTable(
                name: "THANHVIEN_HOIDONG");

            migrationBuilder.DropTable(
                name: "YEUCAU_CHINHSUA");

            migrationBuilder.DropTable(
                name: "YEUCAU_THAYDOI");

            migrationBuilder.DropTable(
                name: "THONGBAO");

            migrationBuilder.DropTable(
                name: "BAOCAO_TIENDO");

            migrationBuilder.DropTable(
                name: "HOIDONG");

            migrationBuilder.DropTable(
                name: "DETAI_DOTDOAN");

            migrationBuilder.DropTable(
                name: "DOAN");

            migrationBuilder.DropTable(
                name: "DANGKYDETAI");

            migrationBuilder.DropTable(
                name: "DETAI");

            migrationBuilder.DropTable(
                name: "DOTDOAN");

            migrationBuilder.DropTable(
                name: "GIANGVIEN");

            migrationBuilder.DropTable(
                name: "SINHVIEN");

            migrationBuilder.DropTable(
                name: "HOCKY");

            migrationBuilder.DropTable(
                name: "HOCHAM");

            migrationBuilder.DropTable(
                name: "HOCVI");

            migrationBuilder.DropTable(
                name: "LOP");

            migrationBuilder.DropTable(
                name: "TAIKHOAN");

            migrationBuilder.DropTable(
                name: "NAMHOC");

            migrationBuilder.DropTable(
                name: "CHUYENNGANH");

            migrationBuilder.DropTable(
                name: "VAITRO");

            migrationBuilder.DropTable(
                name: "BOMON");

            migrationBuilder.DropTable(
                name: "KHOA");
        }
    }
}
