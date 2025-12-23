using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class Add_Location_Geography_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "UserProfiles",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Posts",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Groups",
                type: "geography",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Groups");
        }
    }
}
