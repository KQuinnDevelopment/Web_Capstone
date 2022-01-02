using Microsoft.EntityFrameworkCore.Migrations;

namespace WBSAlpha.Migrations
{
    public partial class ImprovedStanding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Justification",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Justification",
                table: "Standings");
        }
    }
}
