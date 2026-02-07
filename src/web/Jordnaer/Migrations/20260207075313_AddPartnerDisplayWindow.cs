using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerDisplayWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisplayEndUtc",
                table: "Partners",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisplayStartUtc",
                table: "Partners",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayEndUtc",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "DisplayStartUtc",
                table: "Partners");
        }
    }
}
