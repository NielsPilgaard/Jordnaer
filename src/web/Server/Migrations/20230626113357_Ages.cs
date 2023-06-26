using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class Ages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "UserProfiles",
                type: "int",
                nullable: true,
                computedColumnSql: "DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "ChildProfiles",
                type: "int",
                nullable: true,
                computedColumnSql: "DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "ChildProfiles");
        }
    }
}
