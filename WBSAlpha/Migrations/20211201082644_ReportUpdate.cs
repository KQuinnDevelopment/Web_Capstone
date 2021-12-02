using Microsoft.EntityFrameworkCore.Migrations;

namespace WBSAlpha.Migrations
{
    public partial class ReportUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RespondedTo",
                table: "Reports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RespondedTo",
                table: "Reports");
        }
    }
}
