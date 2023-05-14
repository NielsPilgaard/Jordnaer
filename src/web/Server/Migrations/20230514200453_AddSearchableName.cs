using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ZipCode_City",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "SearchableName",
                table: "UserProfiles",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "[FirstName] + [LastName] + [UserName]",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_SearchableName",
                table: "UserProfiles",
                column: "SearchableName");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ZipCode",
                table: "UserProfiles",
                column: "ZipCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_SearchableName",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ZipCode",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SearchableName",
                table: "UserProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ZipCode_City",
                table: "UserProfiles",
                columns: new[] { "ZipCode", "City" });
        }
    }
}
