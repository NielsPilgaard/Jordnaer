using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class Add_ApplicationUser_Cookie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cookie",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cookie",
                table: "AspNetUsers");
        }
    }
}
