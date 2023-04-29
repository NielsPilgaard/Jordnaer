using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileLookingFor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingFor_UserProfiles_UserProfileId",
                table: "LookingFor");

            migrationBuilder.DropIndex(
                name: "IX_LookingFor_UserProfileId",
                table: "LookingFor");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "LookingFor");

            migrationBuilder.CreateTable(
                name: "UserProfileLookingFor",
                columns: table => new
                {
                    UserProfileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LookingForId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileLookingFor", x => new { x.LookingForId, x.UserProfileId });
                    table.ForeignKey(
                        name: "FK_UserProfileLookingFor_LookingFor_LookingForId",
                        column: x => x.LookingForId,
                        principalTable: "LookingFor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileLookingFor_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileLookingFor_UserProfileId",
                table: "UserProfileLookingFor",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfileLookingFor");

            migrationBuilder.AddColumn<string>(
                name: "UserProfileId",
                table: "LookingFor",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingFor_UserProfileId",
                table: "LookingFor",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingFor_UserProfiles_UserProfileId",
                table: "LookingFor",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id");
        }
    }
}
