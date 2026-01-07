using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPartnerFieldsAndAddPendingCardInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PendingMobileImageUrl",
                table: "Partners",
                newName: "PendingLogoUrl");

            migrationBuilder.RenameColumn(
                name: "PendingDesktopImageUrl",
                table: "Partners",
                newName: "PendingLink");

            migrationBuilder.RenameColumn(
                name: "MobileImageUrl",
                table: "Partners",
                newName: "PendingAdImageUrl");

            migrationBuilder.RenameColumn(
                name: "LastImageUpdateUtc",
                table: "Partners",
                newName: "LastUpdateUtc");

            migrationBuilder.RenameColumn(
                name: "HasPendingImageApproval",
                table: "Partners",
                newName: "HasPendingApproval");

            migrationBuilder.RenameColumn(
                name: "DesktopImageUrl",
                table: "Partners",
                newName: "AdImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "PendingDescription",
                table: "Partners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingName",
                table: "Partners",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingDescription",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PendingName",
                table: "Partners");

            migrationBuilder.RenameColumn(
                name: "PendingLogoUrl",
                table: "Partners",
                newName: "PendingMobileImageUrl");

            migrationBuilder.RenameColumn(
                name: "PendingLink",
                table: "Partners",
                newName: "PendingDesktopImageUrl");

            migrationBuilder.RenameColumn(
                name: "PendingAdImageUrl",
                table: "Partners",
                newName: "MobileImageUrl");

            migrationBuilder.RenameColumn(
                name: "LastUpdateUtc",
                table: "Partners",
                newName: "LastImageUpdateUtc");

            migrationBuilder.RenameColumn(
                name: "HasPendingApproval",
                table: "Partners",
                newName: "HasPendingImageApproval");

            migrationBuilder.RenameColumn(
                name: "AdImageUrl",
                table: "Partners",
                newName: "DesktopImageUrl");
        }
    }
}
