using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Migrations
{
    /// <inheritdoc />
    public partial class RenameChildProfilePictureUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PictureUrl",
                table: "ChildProfiles",
                newName: "ProfilePictureUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePictureUrl",
                table: "ChildProfiles",
                newName: "PictureUrl");
        }
    }
}
