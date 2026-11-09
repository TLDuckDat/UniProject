using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class XetGiaoSnapshotExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyDo",
                table: "XetDieuKienChiTiet",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaNgoaiLe",
                table: "XetDieuKienChiTiet",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LyDo",
                table: "XetDieuKienChiTiet");

            migrationBuilder.DropColumn(
                name: "MaNgoaiLe",
                table: "XetDieuKienChiTiet");
        }
    }
}
