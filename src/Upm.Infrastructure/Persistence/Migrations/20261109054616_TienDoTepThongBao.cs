using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TienDoTepThongBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LichNhacViec_SVTrongDotId",
                table: "LichNhacViec");

            migrationBuilder.AddColumn<bool>(
                name: "CamKetAI",
                table: "BaoCaoTienDo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiDiemCamKetAI",
                table: "BaoCaoTienDo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TepDinhKem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuocByte = table.Column<long>(type: "bigint", nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    HashSHA256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NguoiTaoId = table.Column<long>(type: "bigint", nullable: true),
                    NgaySua = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiSuaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDinhKem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TepDinhKem_TaiKhoan_NguoiSuaId",
                        column: x => x.NguoiSuaId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TepDinhKem_TaiKhoan_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<long>(type: "bigint", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NguoiTaoId = table.Column<long>(type: "bigint", nullable: true),
                    NgaySua = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiSuaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_NguoiSuaId",
                        column: x => x.NguoiSuaId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_SVTrongDotId_LoaiNhac_HanNgay",
                table: "LichNhacViec",
                columns: new[] { "SVTrongDotId", "LoaiNhac", "HanNgay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKem_HashSHA256",
                table: "TepDinhKem",
                column: "HashSHA256");

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKem_NguoiSuaId",
                table: "TepDinhKem",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKem_NguoiTaoId",
                table: "TepDinhKem",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiSuaId",
                table: "ThongBao",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiTaoId",
                table: "ThongBao",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_TaiKhoanId_DaDoc",
                table: "ThongBao",
                columns: new[] { "TaiKhoanId", "DaDoc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TepDinhKem");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropIndex(
                name: "IX_LichNhacViec_SVTrongDotId_LoaiNhac_HanNgay",
                table: "LichNhacViec");

            migrationBuilder.DropColumn(
                name: "CamKetAI",
                table: "BaoCaoTienDo");

            migrationBuilder.DropColumn(
                name: "ThoiDiemCamKetAI",
                table: "BaoCaoTienDo");

            migrationBuilder.CreateIndex(
                name: "IX_LichNhacViec_SVTrongDotId",
                table: "LichNhacViec",
                column: "SVTrongDotId");
        }
    }
}
