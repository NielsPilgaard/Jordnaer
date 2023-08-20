using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jordnaer.Server.Migrations
{
    /// <inheritdoc />
    public partial class UnreadMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_LastMessageSentUtc",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChatMessages");

            migrationBuilder.CreateTable(
                name: "UnreadMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageSentUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnreadMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastMessageSentUtc",
                table: "Chats",
                column: "LastMessageSentUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentUtc",
                table: "ChatMessages",
                column: "SentUtc",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnreadMessages");

            migrationBuilder.DropIndex(
                name: "IX_Chats_LastMessageSentUtc",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SentUtc",
                table: "ChatMessages");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastMessageSentUtc",
                table: "Chats",
                column: "LastMessageSentUtc");
        }
    }
}
