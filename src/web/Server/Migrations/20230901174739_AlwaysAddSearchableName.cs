using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AlwaysAddSearchableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SearchableName",
                table: "UserProfiles",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "ISNULL([FirstName], '') + ' ' + ISNULL([LastName], '') + ' ' + ISNULL([UserName], '')",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldComputedColumnSql: "[FirstName] + [LastName] + [UserName]",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SearchableName",
                table: "UserProfiles",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "[FirstName] + [LastName] + [UserName]",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldComputedColumnSql: "ISNULL([FirstName], '') + ' ' + ISNULL([LastName], '') + ' ' + ISNULL([UserName], '')",
                oldStored: true);
        }
    }
}
