using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class XacThuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatKhauHash",
                table: "TaiKhoan",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    HoToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaoLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HetHanLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThuHoiLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoThuHoi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThayTheBoiId = table.Column<long>(type: "bigint", nullable: true),
                    TaoTuIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_RefreshToken_ThayTheBoiId",
                        column: x => x.ThayTheBoiId,
                        principalTable: "RefreshToken",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshToken_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_HoToken",
                table: "RefreshToken",
                column: "HoToken");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TaiKhoanId",
                table: "RefreshToken",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ThayTheBoiId",
                table: "RefreshToken",
                column: "ThayTheBoiId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "MatKhauHash",
                table: "TaiKhoan");
        }
    }
}
