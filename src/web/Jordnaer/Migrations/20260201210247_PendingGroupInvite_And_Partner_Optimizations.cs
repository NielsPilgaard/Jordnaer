using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class PendingGroupInvite_And_Partner_Optimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PendingLink",
                table: "Partners",
                newName: "PendingPartnerPageLink");

            migrationBuilder.RenameColumn(
                name: "Link",
                table: "Partners",
                newName: "PendingAdLink");

            migrationBuilder.AddColumn<string>(
                name: "AdLabelColor",
                table: "Partners",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdLink",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerPageLink",
                table: "Partners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingAdLabelColor",
                table: "Partners",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingGroupInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingGroupInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingGroupInvites_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingGroupInvites_UserProfiles_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingGroupInvites_Email_GroupId",
                table: "PendingGroupInvites",
                columns: new[] { "Email", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingGroupInvites_GroupId",
                table: "PendingGroupInvites",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingGroupInvites_InvitedByUserId",
                table: "PendingGroupInvites",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingGroupInvites_TokenHash",
                table: "PendingGroupInvites",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingGroupInvites");

            migrationBuilder.DropColumn(
                name: "AdLabelColor",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "AdLink",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PartnerPageLink",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PendingAdLabelColor",
                table: "Partners");

            migrationBuilder.RenameColumn(
                name: "PendingPartnerPageLink",
                table: "Partners",
                newName: "PendingLink");

            migrationBuilder.RenameColumn(
                name: "PendingAdLink",
                table: "Partners",
                newName: "Link");
        }
    }
}
