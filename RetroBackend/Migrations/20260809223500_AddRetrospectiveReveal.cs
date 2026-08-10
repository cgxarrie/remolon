using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroBackend.Migrations
{
    [DbContext(typeof(Data.RetroDbContext))]
    [Migration("20260809223500_AddRetrospectiveReveal")]
    public class AddRetrospectiveReveal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevealed",
                table: "Retrospectives",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevealed",
                table: "Retrospectives");
        }
    }
}
