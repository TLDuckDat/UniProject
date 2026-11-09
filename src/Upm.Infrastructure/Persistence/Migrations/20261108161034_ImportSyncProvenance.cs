using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportSyncProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NguonDongBo",
                table: "ThongTinTienDoSV",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DongBoLuc",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguonDongBo",
                table: "SinhVien",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DongBoLuc",
                table: "KyLuatSV",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguonDongBo",
                table: "KyLuatSV",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NguonDongBo",
                table: "ThongTinTienDoSV");

            migrationBuilder.DropColumn(
                name: "DongBoLuc",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NguonDongBo",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "DongBoLuc",
                table: "KyLuatSV");

            migrationBuilder.DropColumn(
                name: "NguonDongBo",
                table: "KyLuatSV");
        }
    }
}
