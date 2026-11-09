using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeTaiKhaiBaoXungDot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhaiBaoXungDot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GiangVienId = table.Column<long>(type: "bigint", nullable: false),
                    SinhVienId = table.Column<long>(type: "bigint", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NguoiTaoId = table.Column<long>(type: "bigint", nullable: true),
                    NgaySua = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiSuaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhaiBaoXungDot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhaiBaoXungDot_GiangVien_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhaiBaoXungDot_SinhVien_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhaiBaoXungDot_TaiKhoan_NguoiSuaId",
                        column: x => x.NguoiSuaId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhaiBaoXungDot_TaiKhoan_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhaiBaoXungDot_GiangVienId_SinhVienId",
                table: "KhaiBaoXungDot",
                columns: new[] { "GiangVienId", "SinhVienId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhaiBaoXungDot_NguoiSuaId",
                table: "KhaiBaoXungDot",
                column: "NguoiSuaId");

            migrationBuilder.CreateIndex(
                name: "IX_KhaiBaoXungDot_NguoiTaoId",
                table: "KhaiBaoXungDot",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_KhaiBaoXungDot_SinhVienId",
                table: "KhaiBaoXungDot",
                column: "SinhVienId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhaiBaoXungDot");
        }
    }
}
