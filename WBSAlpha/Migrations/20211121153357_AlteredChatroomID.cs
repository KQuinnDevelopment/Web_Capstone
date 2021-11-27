using Microsoft.EntityFrameworkCore.Migrations;

namespace WBSAlpha.Migrations
{
    public partial class AlteredChatroomID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chatrooms_ChatID1",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatID1",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChatID1",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "ChatID",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatID",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "ChatID1",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatID1",
                table: "Messages",
                column: "ChatID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chatrooms_ChatID1",
                table: "Messages",
                column: "ChatID1",
                principalTable: "Chatrooms",
                principalColumn: "ChatID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
