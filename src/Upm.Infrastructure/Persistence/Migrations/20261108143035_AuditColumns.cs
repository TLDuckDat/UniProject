using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "YKienGVHD",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "YKienGVHD",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "YKienGVHD",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "YKienGVHD",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "XetDieuKienChiTiet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "XetDieuKienChiTiet",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "XetDieuKienChiTiet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "XetDieuKienChiTiet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "XetDieuKien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "XetDieuKien",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "XetDieuKien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "XetDieuKien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "VaiTro",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "VaiTro",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "VaiTro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "VaiTro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "ThanhVienHoiDong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "ThanhVienHoiDong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "ThanhVienHoiDong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "ThanhVienHoiDong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "ThamSoDaoVan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "ThamSoDaoVan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "ThamSoDaoVan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "ThamSoDaoVan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "ThamSoBoMon",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "ThamSoBoMon",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "ThamSoBoMon",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "ThamSoBoMon",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "TaiKhoanVaiTro",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "TaiKhoanVaiTro",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "TaiKhoanVaiTro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "TaiKhoanVaiTro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "TaiKhoan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "TaiKhoan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "TaiKhoan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "TaiKhoan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "SVTrongDot",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "SVTrongDot",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "SVTrongDot",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "SVTrongDot",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "SinhVien",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "SinhVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "SinhVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "RuleSetKhoaHoc",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "RuleSetKhoaHoc",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "RuleSetKhoaHoc",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "RuleSetKhoaHoc",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "RuleSet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "RuleSet",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "RuleSet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "RuleSet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "QuyetDinhDoiTuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "QuyetDinhDoiTuong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "QuyetDinhDoiTuong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "QuyetDinhDoiTuong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "QuyetDinh",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "QuyetDinh",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "QuyetDinh",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "QuyetDinh",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "PhongBaoVe",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhongBaoVe",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "PhongBaoVe",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "PhongBaoVe",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "PhieuGiaoNhiemVu",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhieuGiaoNhiemVu",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "PhieuGiaoNhiemVu",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "PhieuGiaoNhiemVu",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "PhieuDiem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhieuDiem",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "PhieuDiem",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "PhieuDiem",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "PhienBanQuyen",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhienBanQuyen",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "PhienBanQuyen",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "PhienBanQuyen",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "PhanCong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "PhanCong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "PhanCong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "PhanCong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "MocTienDo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "MocTienDo",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "MocTienDo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "MocTienDo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "LuuTruThuVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "LuuTruThuVien",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "LuuTruThuVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "LuuTruThuVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "LichNhacViec",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "LichNhacViec",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "LichNhacViec",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "LichNhacViec",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "LichBaoVe",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "LichBaoVe",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "LichBaoVe",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "LichBaoVe",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "Khoa",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "Khoa",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "Khoa",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "Khoa",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "KetQuaKiemTraDaoVan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "KetQuaKiemTraDaoVan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "KetQuaKiemTraDaoVan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "KetQuaKiemTraDaoVan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "KetQuaDocDuyet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "KetQuaDocDuyet",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "KetQuaDocDuyet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "KetQuaDocDuyet",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "HoiDong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "HoiDong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "HoiDong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "HoiDong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "HocPhan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "HocPhan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "HocPhan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "HocPhan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "GiangVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "GiangVien",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "GiangVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "GiangVien",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "DotDATN",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "DotDATN",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "DotDATN",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "DotDATN",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "DonTu",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "DonTu",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "DonTu",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "DonTu",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "DiemDATN",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "DiemDATN",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "DiemDATN",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "DiemDATN",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "DeTai",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "DeTai",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "DeTai",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "DeTai",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "DangKyNguyenVong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "DangKyNguyenVong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "DangKyNguyenVong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "DangKyNguyenVong",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "CTDTHocPhan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "CTDTHocPhan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "CTDTHocPhan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "CTDTHocPhan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "CoSo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "CoSo",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "CoSo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "CoSo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "ChuongTrinhDaoTao",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "ChuongTrinhDaoTao",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "ChuongTrinhDaoTao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "ChuongTrinhDaoTao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "BoMon",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "BoMon",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "BoMon",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "BoMon",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "BienBan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "BienBan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "BienBan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "BienBan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "BaoCaoTienDo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "BaoCaoTienDo",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "NguoiSuaId",
                table: "BaoCaoTienDo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NguoiTaoId",
                table: "BaoCaoTienDo",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CoSo",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "CoSo",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "RuleSet",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "RuleSet",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "RuleSetKhoaHoc",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "RuleSetKhoaHoc",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "RuleSetKhoaHoc",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 6L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 7L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "Id",
                keyValue: 8L,
                columns: new[] { "NgaySua", "NgayTao", "NguoiSuaId", "NguoiTaoId" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_YKienGVHD_NguoiSuaId",
                table: "YKienGVHD",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_YKienGVHD_NguoiTaoId",
                table: "YKienGVHD",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKienChiTiet_NguoiSuaId",
                table: "XetDieuKienChiTiet",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKienChiTiet_NguoiTaoId",
                table: "XetDieuKienChiTiet",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKien_NguoiSuaId",
                table: "XetDieuKien",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_XetDieuKien_NguoiTaoId",
                table: "XetDieuKien",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_NguoiSuaId",
                table: "VaiTro",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_NguoiTaoId",
                table: "VaiTro",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienHoiDong_NguoiSuaId",
                table: "ThanhVienHoiDong",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienHoiDong_NguoiTaoId",
                table: "ThanhVienHoiDong",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoDaoVan_NguoiSuaId",
                table: "ThamSoDaoVan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoDaoVan_NguoiTaoId",
                table: "ThamSoDaoVan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoBoMon_NguoiSuaId",
                table: "ThamSoBoMon",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamSoBoMon_NguoiTaoId",
                table: "ThamSoBoMon",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_NguoiSuaId",
                table: "TaiKhoanVaiTro",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_NguoiTaoId",
                table: "TaiKhoanVaiTro",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NguoiSuaId",
                table: "TaiKhoan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NguoiTaoId",
                table: "TaiKhoan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_NguoiSuaId",
                table: "SVTrongDot",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_SVTrongDot_NguoiTaoId",
                table: "SVTrongDot",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_NguoiSuaId",
                table: "SinhVien",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_NguoiTaoId",
                table: "SinhVien",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetKhoaHoc_NguoiSuaId",
                table: "RuleSetKhoaHoc",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetKhoaHoc_NguoiTaoId",
                table: "RuleSetKhoaHoc",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_NguoiSuaId",
                table: "RuleSet",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_NguoiTaoId",
                table: "RuleSet",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_NguoiSuaId",
                table: "QuyetDinhDoiTuong",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhDoiTuong_NguoiTaoId",
                table: "QuyetDinhDoiTuong",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinh_NguoiSuaId",
                table: "QuyetDinh",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinh_NguoiTaoId",
                table: "QuyetDinh",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhongBaoVe_NguoiSuaId",
                table: "PhongBaoVe",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhongBaoVe_NguoiTaoId",
                table: "PhongBaoVe",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuGiaoNhiemVu_NguoiSuaId",
                table: "PhieuGiaoNhiemVu",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuGiaoNhiemVu_NguoiTaoId",
                table: "PhieuGiaoNhiemVu",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiem_NguoiSuaId",
                table: "PhieuDiem",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDiem_NguoiTaoId",
                table: "PhieuDiem",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhienBanQuyen_NguoiSuaId",
                table: "PhienBanQuyen",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhienBanQuyen_NguoiTaoId",
                table: "PhienBanQuyen",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_NguoiSuaId",
                table: "PhanCong",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCong_NguoiTaoId",
                table: "PhanCong",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_MocTienDo_NguoiSuaId",
                table: "MocTienDo",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_MocTienDo_NguoiTaoId",
                table: "MocTienDo",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruThuVien_NguoiSuaId",
                table: "LuuTruThuVien",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruThuVien_NguoiTaoId",
                table: "LuuTruThuVien",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_NguoiSuaId",
                table: "LichNhacViec",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_NguoiTaoId",
                table: "LichNhacViec",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoVe_NguoiSuaId",
                table: "LichBaoVe",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoVe_NguoiTaoId",
                table: "LichBaoVe",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Khoa_NguoiSuaId",
                table: "Khoa",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_Khoa_NguoiTaoId",
                table: "Khoa",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaKiemTraDaoVan_NguoiSuaId",
                table: "KetQuaKiemTraDaoVan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaKiemTraDaoVan_NguoiTaoId",
                table: "KetQuaKiemTraDaoVan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDocDuyet_NguoiSuaId",
                table: "KetQuaDocDuyet",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDocDuyet_NguoiTaoId",
                table: "KetQuaDocDuyet",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_NguoiSuaId",
                table: "HoiDong",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_HoiDong_NguoiTaoId",
                table: "HoiDong",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhan_NguoiSuaId",
                table: "HocPhan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhan_NguoiTaoId",
                table: "HocPhan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangVien_NguoiSuaId",
                table: "GiangVien",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangVien_NguoiTaoId",
                table: "GiangVien",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDATN_NguoiSuaId",
                table: "DotDATN",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDATN_NguoiTaoId",
                table: "DotDATN",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_NguoiSuaId",
                table: "DonTu",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DonTu_NguoiTaoId",
                table: "DonTu",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_NguoiSuaId",
                table: "DiemDATN",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDATN_NguoiTaoId",
                table: "DiemDATN",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_NguoiSuaId",
                table: "DeTai",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DeTai_NguoiTaoId",
                table: "DeTai",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyNguyenVong_NguoiSuaId",
                table: "DangKyNguyenVong",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyNguyenVong_NguoiTaoId",
                table: "DangKyNguyenVong",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CTDTHocPhan_NguoiSuaId",
                table: "CTDTHocPhan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_CTDTHocPhan_NguoiTaoId",
                table: "CTDTHocPhan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CoSo_NguoiSuaId",
                table: "CoSo",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_CoSo_NguoiTaoId",
                table: "CoSo",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTao_NguoiSuaId",
                table: "ChuongTrinhDaoTao",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTao_NguoiTaoId",
                table: "ChuongTrinhDaoTao",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BoMon_NguoiSuaId",
                table: "BoMon",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_BoMon_NguoiTaoId",
                table: "BoMon",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BienBan_NguoiSuaId",
                table: "BienBan",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_BienBan_NguoiTaoId",
                table: "BienBan",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTienDo_NguoiSuaId",
                table: "BaoCaoTienDo",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTienDo_NguoiTaoId",
                table: "BaoCaoTienDo",
                column: "NguoiTaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_BaoCaoTienDo_TaiKhoan_NguoiSuaId",
                table: "BaoCaoTienDo",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaoCaoTienDo_TaiKhoan_NguoiTaoId",
                table: "BaoCaoTienDo",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BienBan_TaiKhoan_NguoiSuaId",
                table: "BienBan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BienBan_TaiKhoan_NguoiTaoId",
                table: "BienBan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoMon_TaiKhoan_NguoiSuaId",
                table: "BoMon",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoMon_TaiKhoan_NguoiTaoId",
                table: "BoMon",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChuongTrinhDaoTao_TaiKhoan_NguoiSuaId",
                table: "ChuongTrinhDaoTao",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChuongTrinhDaoTao_TaiKhoan_NguoiTaoId",
                table: "ChuongTrinhDaoTao",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoSo_TaiKhoan_NguoiSuaId",
                table: "CoSo",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoSo_TaiKhoan_NguoiTaoId",
                table: "CoSo",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CTDTHocPhan_TaiKhoan_NguoiSuaId",
                table: "CTDTHocPhan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CTDTHocPhan_TaiKhoan_NguoiTaoId",
                table: "CTDTHocPhan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyNguyenVong_TaiKhoan_NguoiSuaId",
                table: "DangKyNguyenVong",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyNguyenVong_TaiKhoan_NguoiTaoId",
                table: "DangKyNguyenVong",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeTai_TaiKhoan_NguoiSuaId",
                table: "DeTai",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeTai_TaiKhoan_NguoiTaoId",
                table: "DeTai",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiemDATN_TaiKhoan_NguoiSuaId",
                table: "DiemDATN",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiemDATN_TaiKhoan_NguoiTaoId",
                table: "DiemDATN",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonTu_TaiKhoan_NguoiSuaId",
                table: "DonTu",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonTu_TaiKhoan_NguoiTaoId",
                table: "DonTu",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DotDATN_TaiKhoan_NguoiSuaId",
                table: "DotDATN",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DotDATN_TaiKhoan_NguoiTaoId",
                table: "DotDATN",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GiangVien_TaiKhoan_NguoiSuaId",
                table: "GiangVien",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GiangVien_TaiKhoan_NguoiTaoId",
                table: "GiangVien",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HocPhan_TaiKhoan_NguoiSuaId",
                table: "HocPhan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HocPhan_TaiKhoan_NguoiTaoId",
                table: "HocPhan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoiDong_TaiKhoan_NguoiSuaId",
                table: "HoiDong",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoiDong_TaiKhoan_NguoiTaoId",
                table: "HoiDong",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaDocDuyet_TaiKhoan_NguoiSuaId",
                table: "KetQuaDocDuyet",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaDocDuyet_TaiKhoan_NguoiTaoId",
                table: "KetQuaDocDuyet",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaKiemTraDaoVan_TaiKhoan_NguoiSuaId",
                table: "KetQuaKiemTraDaoVan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaKiemTraDaoVan_TaiKhoan_NguoiTaoId",
                table: "KetQuaKiemTraDaoVan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Khoa_TaiKhoan_NguoiSuaId",
                table: "Khoa",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Khoa_TaiKhoan_NguoiTaoId",
                table: "Khoa",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichBaoVe_TaiKhoan_NguoiSuaId",
                table: "LichBaoVe",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichBaoVe_TaiKhoan_NguoiTaoId",
                table: "LichBaoVe",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichNhacViec_TaiKhoan_NguoiSuaId",
                table: "LichNhacViec",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichNhacViec_TaiKhoan_NguoiTaoId",
                table: "LichNhacViec",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LuuTruThuVien_TaiKhoan_NguoiSuaId",
                table: "LuuTruThuVien",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LuuTruThuVien_TaiKhoan_NguoiTaoId",
                table: "LuuTruThuVien",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MocTienDo_TaiKhoan_NguoiSuaId",
                table: "MocTienDo",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MocTienDo_TaiKhoan_NguoiTaoId",
                table: "MocTienDo",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCong_TaiKhoan_NguoiSuaId",
                table: "PhanCong",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCong_TaiKhoan_NguoiTaoId",
                table: "PhanCong",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhienBanQuyen_TaiKhoan_NguoiSuaId",
                table: "PhienBanQuyen",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhienBanQuyen_TaiKhoan_NguoiTaoId",
                table: "PhienBanQuyen",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuDiem_TaiKhoan_NguoiSuaId",
                table: "PhieuDiem",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuDiem_TaiKhoan_NguoiTaoId",
                table: "PhieuDiem",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuGiaoNhiemVu_TaiKhoan_NguoiSuaId",
                table: "PhieuGiaoNhiemVu",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuGiaoNhiemVu_TaiKhoan_NguoiTaoId",
                table: "PhieuGiaoNhiemVu",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongBaoVe_TaiKhoan_NguoiSuaId",
                table: "PhongBaoVe",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongBaoVe_TaiKhoan_NguoiTaoId",
                table: "PhongBaoVe",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyetDinh_TaiKhoan_NguoiSuaId",
                table: "QuyetDinh",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyetDinh_TaiKhoan_NguoiTaoId",
                table: "QuyetDinh",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyetDinhDoiTuong_TaiKhoan_NguoiSuaId",
                table: "QuyetDinhDoiTuong",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuyetDinhDoiTuong_TaiKhoan_NguoiTaoId",
                table: "QuyetDinhDoiTuong",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleSet_TaiKhoan_NguoiSuaId",
                table: "RuleSet",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleSet_TaiKhoan_NguoiTaoId",
                table: "RuleSet",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleSetKhoaHoc_TaiKhoan_NguoiSuaId",
                table: "RuleSetKhoaHoc",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleSetKhoaHoc_TaiKhoan_NguoiTaoId",
                table: "RuleSetKhoaHoc",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SinhVien_TaiKhoan_NguoiSuaId",
                table: "SinhVien",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SinhVien_TaiKhoan_NguoiTaoId",
                table: "SinhVien",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SVTrongDot_TaiKhoan_NguoiSuaId",
                table: "SVTrongDot",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SVTrongDot_TaiKhoan_NguoiTaoId",
                table: "SVTrongDot",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoan_TaiKhoan_NguoiSuaId",
                table: "TaiKhoan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoan_TaiKhoan_NguoiTaoId",
                table: "TaiKhoan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanVaiTro_TaiKhoan_NguoiSuaId",
                table: "TaiKhoanVaiTro",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanVaiTro_TaiKhoan_NguoiTaoId",
                table: "TaiKhoanVaiTro",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThamSoBoMon_TaiKhoan_NguoiSuaId",
                table: "ThamSoBoMon",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThamSoBoMon_TaiKhoan_NguoiTaoId",
                table: "ThamSoBoMon",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThamSoDaoVan_TaiKhoan_NguoiSuaId",
                table: "ThamSoDaoVan",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThamSoDaoVan_TaiKhoan_NguoiTaoId",
                table: "ThamSoDaoVan",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhVienHoiDong_TaiKhoan_NguoiSuaId",
                table: "ThanhVienHoiDong",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhVienHoiDong_TaiKhoan_NguoiTaoId",
                table: "ThanhVienHoiDong",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VaiTro_TaiKhoan_NguoiSuaId",
                table: "VaiTro",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VaiTro_TaiKhoan_NguoiTaoId",
                table: "VaiTro",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_XetDieuKien_TaiKhoan_NguoiSuaId",
                table: "XetDieuKien",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_XetDieuKien_TaiKhoan_NguoiTaoId",
                table: "XetDieuKien",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_XetDieuKienChiTiet_TaiKhoan_NguoiSuaId",
                table: "XetDieuKienChiTiet",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_XetDieuKienChiTiet_TaiKhoan_NguoiTaoId",
                table: "XetDieuKienChiTiet",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YKienGVHD_TaiKhoan_NguoiSuaId",
                table: "YKienGVHD",
                column: "NguoiSuaId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YKienGVHD_TaiKhoan_NguoiTaoId",
                table: "YKienGVHD",
                column: "NguoiTaoId",
                principalTable: "TaiKhoan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaoCaoTienDo_TaiKhoan_NguoiSuaId",
                table: "BaoCaoTienDo");

            migrationBuilder.DropForeignKey(
                name: "FK_BaoCaoTienDo_TaiKhoan_NguoiTaoId",
                table: "BaoCaoTienDo");

            migrationBuilder.DropForeignKey(
                name: "FK_BienBan_TaiKhoan_NguoiSuaId",
                table: "BienBan");

            migrationBuilder.DropForeignKey(
                name: "FK_BienBan_TaiKhoan_NguoiTaoId",
                table: "BienBan");

            migrationBuilder.DropForeignKey(
                name: "FK_BoMon_TaiKhoan_NguoiSuaId",
                table: "BoMon");

            migrationBuilder.DropForeignKey(
                name: "FK_BoMon_TaiKhoan_NguoiTaoId",
                table: "BoMon");

            migrationBuilder.DropForeignKey(
                name: "FK_ChuongTrinhDaoTao_TaiKhoan_NguoiSuaId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropForeignKey(
                name: "FK_ChuongTrinhDaoTao_TaiKhoan_NguoiTaoId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropForeignKey(
                name: "FK_CoSo_TaiKhoan_NguoiSuaId",
                table: "CoSo");

            migrationBuilder.DropForeignKey(
                name: "FK_CoSo_TaiKhoan_NguoiTaoId",
                table: "CoSo");

            migrationBuilder.DropForeignKey(
                name: "FK_CTDTHocPhan_TaiKhoan_NguoiSuaId",
                table: "CTDTHocPhan");

            migrationBuilder.DropForeignKey(
                name: "FK_CTDTHocPhan_TaiKhoan_NguoiTaoId",
                table: "CTDTHocPhan");

            migrationBuilder.DropForeignKey(
                name: "FK_DangKyNguyenVong_TaiKhoan_NguoiSuaId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropForeignKey(
                name: "FK_DangKyNguyenVong_TaiKhoan_NguoiTaoId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropForeignKey(
                name: "FK_DeTai_TaiKhoan_NguoiSuaId",
                table: "DeTai");

            migrationBuilder.DropForeignKey(
                name: "FK_DeTai_TaiKhoan_NguoiTaoId",
                table: "DeTai");

            migrationBuilder.DropForeignKey(
                name: "FK_DiemDATN_TaiKhoan_NguoiSuaId",
                table: "DiemDATN");

            migrationBuilder.DropForeignKey(
                name: "FK_DiemDATN_TaiKhoan_NguoiTaoId",
                table: "DiemDATN");

            migrationBuilder.DropForeignKey(
                name: "FK_DonTu_TaiKhoan_NguoiSuaId",
                table: "DonTu");

            migrationBuilder.DropForeignKey(
                name: "FK_DonTu_TaiKhoan_NguoiTaoId",
                table: "DonTu");

            migrationBuilder.DropForeignKey(
                name: "FK_DotDATN_TaiKhoan_NguoiSuaId",
                table: "DotDATN");

            migrationBuilder.DropForeignKey(
                name: "FK_DotDATN_TaiKhoan_NguoiTaoId",
                table: "DotDATN");

            migrationBuilder.DropForeignKey(
                name: "FK_GiangVien_TaiKhoan_NguoiSuaId",
                table: "GiangVien");

            migrationBuilder.DropForeignKey(
                name: "FK_GiangVien_TaiKhoan_NguoiTaoId",
                table: "GiangVien");

            migrationBuilder.DropForeignKey(
                name: "FK_HocPhan_TaiKhoan_NguoiSuaId",
                table: "HocPhan");

            migrationBuilder.DropForeignKey(
                name: "FK_HocPhan_TaiKhoan_NguoiTaoId",
                table: "HocPhan");

            migrationBuilder.DropForeignKey(
                name: "FK_HoiDong_TaiKhoan_NguoiSuaId",
                table: "HoiDong");

            migrationBuilder.DropForeignKey(
                name: "FK_HoiDong_TaiKhoan_NguoiTaoId",
                table: "HoiDong");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaDocDuyet_TaiKhoan_NguoiSuaId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaDocDuyet_TaiKhoan_NguoiTaoId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaKiemTraDaoVan_TaiKhoan_NguoiSuaId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaKiemTraDaoVan_TaiKhoan_NguoiTaoId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropForeignKey(
                name: "FK_Khoa_TaiKhoan_NguoiSuaId",
                table: "Khoa");

            migrationBuilder.DropForeignKey(
                name: "FK_Khoa_TaiKhoan_NguoiTaoId",
                table: "Khoa");

            migrationBuilder.DropForeignKey(
                name: "FK_LichBaoVe_TaiKhoan_NguoiSuaId",
                table: "LichBaoVe");

            migrationBuilder.DropForeignKey(
                name: "FK_LichBaoVe_TaiKhoan_NguoiTaoId",
                table: "LichBaoVe");

            migrationBuilder.DropForeignKey(
                name: "FK_LichNhacViec_TaiKhoan_NguoiSuaId",
                table: "LichNhacViec");

            migrationBuilder.DropForeignKey(
                name: "FK_LichNhacViec_TaiKhoan_NguoiTaoId",
                table: "LichNhacViec");

            migrationBuilder.DropForeignKey(
                name: "FK_LuuTruThuVien_TaiKhoan_NguoiSuaId",
                table: "LuuTruThuVien");

            migrationBuilder.DropForeignKey(
                name: "FK_LuuTruThuVien_TaiKhoan_NguoiTaoId",
                table: "LuuTruThuVien");

            migrationBuilder.DropForeignKey(
                name: "FK_MocTienDo_TaiKhoan_NguoiSuaId",
                table: "MocTienDo");

            migrationBuilder.DropForeignKey(
                name: "FK_MocTienDo_TaiKhoan_NguoiTaoId",
                table: "MocTienDo");

            migrationBuilder.DropForeignKey(
                name: "FK_PhanCong_TaiKhoan_NguoiSuaId",
                table: "PhanCong");

            migrationBuilder.DropForeignKey(
                name: "FK_PhanCong_TaiKhoan_NguoiTaoId",
                table: "PhanCong");

            migrationBuilder.DropForeignKey(
                name: "FK_PhienBanQuyen_TaiKhoan_NguoiSuaId",
                table: "PhienBanQuyen");

            migrationBuilder.DropForeignKey(
                name: "FK_PhienBanQuyen_TaiKhoan_NguoiTaoId",
                table: "PhienBanQuyen");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuDiem_TaiKhoan_NguoiSuaId",
                table: "PhieuDiem");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuDiem_TaiKhoan_NguoiTaoId",
                table: "PhieuDiem");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuGiaoNhiemVu_TaiKhoan_NguoiSuaId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuGiaoNhiemVu_TaiKhoan_NguoiTaoId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongBaoVe_TaiKhoan_NguoiSuaId",
                table: "PhongBaoVe");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongBaoVe_TaiKhoan_NguoiTaoId",
                table: "PhongBaoVe");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyetDinh_TaiKhoan_NguoiSuaId",
                table: "QuyetDinh");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyetDinh_TaiKhoan_NguoiTaoId",
                table: "QuyetDinh");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyetDinhDoiTuong_TaiKhoan_NguoiSuaId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropForeignKey(
                name: "FK_QuyetDinhDoiTuong_TaiKhoan_NguoiTaoId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleSet_TaiKhoan_NguoiSuaId",
                table: "RuleSet");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleSet_TaiKhoan_NguoiTaoId",
                table: "RuleSet");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleSetKhoaHoc_TaiKhoan_NguoiSuaId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleSetKhoaHoc_TaiKhoan_NguoiTaoId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropForeignKey(
                name: "FK_SinhVien_TaiKhoan_NguoiSuaId",
                table: "SinhVien");

            migrationBuilder.DropForeignKey(
                name: "FK_SinhVien_TaiKhoan_NguoiTaoId",
                table: "SinhVien");

            migrationBuilder.DropForeignKey(
                name: "FK_SVTrongDot_TaiKhoan_NguoiSuaId",
                table: "SVTrongDot");

            migrationBuilder.DropForeignKey(
                name: "FK_SVTrongDot_TaiKhoan_NguoiTaoId",
                table: "SVTrongDot");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoan_TaiKhoan_NguoiSuaId",
                table: "TaiKhoan");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoan_TaiKhoan_NguoiTaoId",
                table: "TaiKhoan");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoanVaiTro_TaiKhoan_NguoiSuaId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoanVaiTro_TaiKhoan_NguoiTaoId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropForeignKey(
                name: "FK_ThamSoBoMon_TaiKhoan_NguoiSuaId",
                table: "ThamSoBoMon");

            migrationBuilder.DropForeignKey(
                name: "FK_ThamSoBoMon_TaiKhoan_NguoiTaoId",
                table: "ThamSoBoMon");

            migrationBuilder.DropForeignKey(
                name: "FK_ThamSoDaoVan_TaiKhoan_NguoiSuaId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropForeignKey(
                name: "FK_ThamSoDaoVan_TaiKhoan_NguoiTaoId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhVienHoiDong_TaiKhoan_NguoiSuaId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhVienHoiDong_TaiKhoan_NguoiTaoId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropForeignKey(
                name: "FK_VaiTro_TaiKhoan_NguoiSuaId",
                table: "VaiTro");

            migrationBuilder.DropForeignKey(
                name: "FK_VaiTro_TaiKhoan_NguoiTaoId",
                table: "VaiTro");

            migrationBuilder.DropForeignKey(
                name: "FK_XetDieuKien_TaiKhoan_NguoiSuaId",
                table: "XetDieuKien");

            migrationBuilder.DropForeignKey(
                name: "FK_XetDieuKien_TaiKhoan_NguoiTaoId",
                table: "XetDieuKien");

            migrationBuilder.DropForeignKey(
                name: "FK_XetDieuKienChiTiet_TaiKhoan_NguoiSuaId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropForeignKey(
                name: "FK_XetDieuKienChiTiet_TaiKhoan_NguoiTaoId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropForeignKey(
                name: "FK_YKienGVHD_TaiKhoan_NguoiSuaId",
                table: "YKienGVHD");

            migrationBuilder.DropForeignKey(
                name: "FK_YKienGVHD_TaiKhoan_NguoiTaoId",
                table: "YKienGVHD");

            migrationBuilder.DropIndex(
                name: "IX_YKienGVHD_NguoiSuaId",
                table: "YKienGVHD");

            migrationBuilder.DropIndex(
                name: "IX_YKienGVHD_NguoiTaoId",
                table: "YKienGVHD");

            migrationBuilder.DropIndex(
                name: "IX_XetDieuKienChiTiet_NguoiSuaId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropIndex(
                name: "IX_XetDieuKienChiTiet_NguoiTaoId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropIndex(
                name: "IX_XetDieuKien_NguoiSuaId",
                table: "XetDieuKien");

            migrationBuilder.DropIndex(
                name: "IX_XetDieuKien_NguoiTaoId",
                table: "XetDieuKien");

            migrationBuilder.DropIndex(
                name: "IX_VaiTro_NguoiSuaId",
                table: "VaiTro");

            migrationBuilder.DropIndex(
                name: "IX_VaiTro_NguoiTaoId",
                table: "VaiTro");

            migrationBuilder.DropIndex(
                name: "IX_ThanhVienHoiDong_NguoiSuaId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropIndex(
                name: "IX_ThanhVienHoiDong_NguoiTaoId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropIndex(
                name: "IX_ThamSoDaoVan_NguoiSuaId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropIndex(
                name: "IX_ThamSoDaoVan_NguoiTaoId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropIndex(
                name: "IX_ThamSoBoMon_NguoiSuaId",
                table: "ThamSoBoMon");

            migrationBuilder.DropIndex(
                name: "IX_ThamSoBoMon_NguoiTaoId",
                table: "ThamSoBoMon");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoanVaiTro_NguoiSuaId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoanVaiTro_NguoiTaoId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_NguoiSuaId",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_NguoiTaoId",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_SVTrongDot_NguoiSuaId",
                table: "SVTrongDot");

            migrationBuilder.DropIndex(
                name: "IX_SVTrongDot_NguoiTaoId",
                table: "SVTrongDot");

            migrationBuilder.DropIndex(
                name: "IX_SinhVien_NguoiSuaId",
                table: "SinhVien");

            migrationBuilder.DropIndex(
                name: "IX_SinhVien_NguoiTaoId",
                table: "SinhVien");

            migrationBuilder.DropIndex(
                name: "IX_RuleSetKhoaHoc_NguoiSuaId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropIndex(
                name: "IX_RuleSetKhoaHoc_NguoiTaoId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropIndex(
                name: "IX_RuleSet_NguoiSuaId",
                table: "RuleSet");

            migrationBuilder.DropIndex(
                name: "IX_RuleSet_NguoiTaoId",
                table: "RuleSet");

            migrationBuilder.DropIndex(
                name: "IX_QuyetDinhDoiTuong_NguoiSuaId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropIndex(
                name: "IX_QuyetDinhDoiTuong_NguoiTaoId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropIndex(
                name: "IX_QuyetDinh_NguoiSuaId",
                table: "QuyetDinh");

            migrationBuilder.DropIndex(
                name: "IX_QuyetDinh_NguoiTaoId",
                table: "QuyetDinh");

            migrationBuilder.DropIndex(
                name: "IX_PhongBaoVe_NguoiSuaId",
                table: "PhongBaoVe");

            migrationBuilder.DropIndex(
                name: "IX_PhongBaoVe_NguoiTaoId",
                table: "PhongBaoVe");

            migrationBuilder.DropIndex(
                name: "IX_PhieuGiaoNhiemVu_NguoiSuaId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropIndex(
                name: "IX_PhieuGiaoNhiemVu_NguoiTaoId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropIndex(
                name: "IX_PhieuDiem_NguoiSuaId",
                table: "PhieuDiem");

            migrationBuilder.DropIndex(
                name: "IX_PhieuDiem_NguoiTaoId",
                table: "PhieuDiem");

            migrationBuilder.DropIndex(
                name: "IX_PhienBanQuyen_NguoiSuaId",
                table: "PhienBanQuyen");

            migrationBuilder.DropIndex(
                name: "IX_PhienBanQuyen_NguoiTaoId",
                table: "PhienBanQuyen");

            migrationBuilder.DropIndex(
                name: "IX_PhanCong_NguoiSuaId",
                table: "PhanCong");

            migrationBuilder.DropIndex(
                name: "IX_PhanCong_NguoiTaoId",
                table: "PhanCong");

            migrationBuilder.DropIndex(
                name: "IX_MocTienDo_NguoiSuaId",
                table: "MocTienDo");

            migrationBuilder.DropIndex(
                name: "IX_MocTienDo_NguoiTaoId",
                table: "MocTienDo");

            migrationBuilder.DropIndex(
                name: "IX_LuuTruThuVien_NguoiSuaId",
                table: "LuuTruThuVien");

            migrationBuilder.DropIndex(
                name: "IX_LuuTruThuVien_NguoiTaoId",
                table: "LuuTruThuVien");

            migrationBuilder.DropIndex(
                name: "IX_LichNhacViec_NguoiSuaId",
                table: "LichNhacViec");

            migrationBuilder.DropIndex(
                name: "IX_LichNhacViec_NguoiTaoId",
                table: "LichNhacViec");

            migrationBuilder.DropIndex(
                name: "IX_LichBaoVe_NguoiSuaId",
                table: "LichBaoVe");

            migrationBuilder.DropIndex(
                name: "IX_LichBaoVe_NguoiTaoId",
                table: "LichBaoVe");

            migrationBuilder.DropIndex(
                name: "IX_Khoa_NguoiSuaId",
                table: "Khoa");

            migrationBuilder.DropIndex(
                name: "IX_Khoa_NguoiTaoId",
                table: "Khoa");

            migrationBuilder.DropIndex(
                name: "IX_KetQuaKiemTraDaoVan_NguoiSuaId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropIndex(
                name: "IX_KetQuaKiemTraDaoVan_NguoiTaoId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropIndex(
                name: "IX_KetQuaDocDuyet_NguoiSuaId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropIndex(
                name: "IX_KetQuaDocDuyet_NguoiTaoId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropIndex(
                name: "IX_HoiDong_NguoiSuaId",
                table: "HoiDong");

            migrationBuilder.DropIndex(
                name: "IX_HoiDong_NguoiTaoId",
                table: "HoiDong");

            migrationBuilder.DropIndex(
                name: "IX_HocPhan_NguoiSuaId",
                table: "HocPhan");

            migrationBuilder.DropIndex(
                name: "IX_HocPhan_NguoiTaoId",
                table: "HocPhan");

            migrationBuilder.DropIndex(
                name: "IX_GiangVien_NguoiSuaId",
                table: "GiangVien");

            migrationBuilder.DropIndex(
                name: "IX_GiangVien_NguoiTaoId",
                table: "GiangVien");

            migrationBuilder.DropIndex(
                name: "IX_DotDATN_NguoiSuaId",
                table: "DotDATN");

            migrationBuilder.DropIndex(
                name: "IX_DotDATN_NguoiTaoId",
                table: "DotDATN");

            migrationBuilder.DropIndex(
                name: "IX_DonTu_NguoiSuaId",
                table: "DonTu");

            migrationBuilder.DropIndex(
                name: "IX_DonTu_NguoiTaoId",
                table: "DonTu");

            migrationBuilder.DropIndex(
                name: "IX_DiemDATN_NguoiSuaId",
                table: "DiemDATN");

            migrationBuilder.DropIndex(
                name: "IX_DiemDATN_NguoiTaoId",
                table: "DiemDATN");

            migrationBuilder.DropIndex(
                name: "IX_DeTai_NguoiSuaId",
                table: "DeTai");

            migrationBuilder.DropIndex(
                name: "IX_DeTai_NguoiTaoId",
                table: "DeTai");

            migrationBuilder.DropIndex(
                name: "IX_DangKyNguyenVong_NguoiSuaId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropIndex(
                name: "IX_DangKyNguyenVong_NguoiTaoId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropIndex(
                name: "IX_CTDTHocPhan_NguoiSuaId",
                table: "CTDTHocPhan");

            migrationBuilder.DropIndex(
                name: "IX_CTDTHocPhan_NguoiTaoId",
                table: "CTDTHocPhan");

            migrationBuilder.DropIndex(
                name: "IX_CoSo_NguoiSuaId",
                table: "CoSo");

            migrationBuilder.DropIndex(
                name: "IX_CoSo_NguoiTaoId",
                table: "CoSo");

            migrationBuilder.DropIndex(
                name: "IX_ChuongTrinhDaoTao_NguoiSuaId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropIndex(
                name: "IX_ChuongTrinhDaoTao_NguoiTaoId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropIndex(
                name: "IX_BoMon_NguoiSuaId",
                table: "BoMon");

            migrationBuilder.DropIndex(
                name: "IX_BoMon_NguoiTaoId",
                table: "BoMon");

            migrationBuilder.DropIndex(
                name: "IX_BienBan_NguoiSuaId",
                table: "BienBan");

            migrationBuilder.DropIndex(
                name: "IX_BienBan_NguoiTaoId",
                table: "BienBan");

            migrationBuilder.DropIndex(
                name: "IX_BaoCaoTienDo_NguoiSuaId",
                table: "BaoCaoTienDo");

            migrationBuilder.DropIndex(
                name: "IX_BaoCaoTienDo_NguoiTaoId",
                table: "BaoCaoTienDo");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "YKienGVHD");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "YKienGVHD");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "YKienGVHD");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "YKienGVHD");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "XetDieuKien");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "XetDieuKien");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "XetDieuKien");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "XetDieuKien");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "VaiTro");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "VaiTro");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "VaiTro");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "VaiTro");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "ThanhVienHoiDong");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "ThamSoDaoVan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "ThamSoDaoVan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "ThamSoDaoVan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "ThamSoBoMon");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "ThamSoBoMon");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "ThamSoBoMon");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "ThamSoBoMon");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "TaiKhoanVaiTro");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "SVTrongDot");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "SVTrongDot");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "SVTrongDot");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "SVTrongDot");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "RuleSetKhoaHoc");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "RuleSet");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "RuleSet");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "RuleSet");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "RuleSet");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "QuyetDinhDoiTuong");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "QuyetDinh");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "QuyetDinh");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "QuyetDinh");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "QuyetDinh");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "PhongBaoVe");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhongBaoVe");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "PhongBaoVe");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "PhongBaoVe");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "PhieuGiaoNhiemVu");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "PhieuDiem");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhieuDiem");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "PhieuDiem");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "PhieuDiem");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "PhienBanQuyen");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhienBanQuyen");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "PhienBanQuyen");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "PhienBanQuyen");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "PhanCong");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "PhanCong");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "PhanCong");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "PhanCong");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "MocTienDo");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "MocTienDo");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "MocTienDo");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "MocTienDo");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "LuuTruThuVien");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "LuuTruThuVien");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "LuuTruThuVien");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "LuuTruThuVien");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "LichNhacViec");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "LichNhacViec");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "LichNhacViec");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "LichNhacViec");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "LichBaoVe");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "LichBaoVe");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "LichBaoVe");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "LichBaoVe");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "KetQuaKiemTraDaoVan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "KetQuaDocDuyet");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "HoiDong");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "HoiDong");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "HoiDong");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "HoiDong");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "HocPhan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "HocPhan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "HocPhan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "HocPhan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "DotDATN");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "DotDATN");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "DotDATN");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "DotDATN");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "DonTu");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "DonTu");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "DonTu");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "DonTu");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "DiemDATN");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "DiemDATN");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "DiemDATN");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "DiemDATN");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "DeTai");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "DeTai");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "DeTai");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "DeTai");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "DangKyNguyenVong");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "DangKyNguyenVong");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "DangKyNguyenVong");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "CTDTHocPhan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "CTDTHocPhan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "CTDTHocPhan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "CTDTHocPhan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "CoSo");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "CoSo");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "CoSo");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "CoSo");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "ChuongTrinhDaoTao");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "BoMon");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "BoMon");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "BoMon");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "BoMon");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "BienBan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "BienBan");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "BienBan");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "BienBan");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "BaoCaoTienDo");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "BaoCaoTienDo");

            migrationBuilder.DropColumn(
                name: "NguoiSuaId",
                table: "BaoCaoTienDo");

            migrationBuilder.DropColumn(
                name: "NguoiTaoId",
                table: "BaoCaoTienDo");
        }
    }
}
