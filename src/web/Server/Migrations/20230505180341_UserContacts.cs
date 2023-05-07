using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class UserContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_UserProfiles_UserProfileId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserProfileId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "UserProfiles");

            migrationBuilder.CreateTable(
                name: "UserContacts",
                columns: table => new
                {
                    UserProfileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContactId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContacts", x => new { x.ContactId, x.UserProfileId });
                    table.ForeignKey(
                        name: "FK_UserContacts_UserProfiles_ContactId",
                        column: x => x.ContactId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserContacts_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_DateOfBirth",
                table: "ChildProfiles",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_Gender",
                table: "ChildProfiles",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_UserContacts_UserProfileId",
                table: "UserContacts",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserContacts");

            migrationBuilder.DropIndex(
                name: "IX_ChildProfiles_DateOfBirth",
                table: "ChildProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ChildProfiles_Gender",
                table: "ChildProfiles");

            migrationBuilder.AddColumn<string>(
                name: "UserProfileId",
                table: "UserProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserProfileId",
                table: "UserProfiles",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_UserProfiles_UserProfileId",
                table: "UserProfiles",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id");
        }
    }
}
