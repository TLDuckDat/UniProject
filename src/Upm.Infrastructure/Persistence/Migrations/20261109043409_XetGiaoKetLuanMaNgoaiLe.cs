using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class XetGiaoKetLuanMaNgoaiLe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaNgoaiLe",
                table: "XetDieuKien",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaNgoaiLe",
                table: "XetDieuKien");
        }
    }
}
