using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class ConvertZipCodeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                table: "UserProfiles",
                newName: "Address");

            migrationBuilder.AlterColumn<int>(
                name: "ZipCode",
                table: "UserProfiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "UserProfiles",
                newName: "Location");

            migrationBuilder.AlterColumn<string>(
                name: "ZipCode",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
