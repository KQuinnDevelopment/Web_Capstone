using Microsoft.EntityFrameworkCore.Migrations;

namespace WBSAlpha.Migrations
{
    public partial class BuildRatingUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Votes",
                table: "GameOneBuilds",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Votes",
                table: "GameOneBuilds");
        }
    }
}
