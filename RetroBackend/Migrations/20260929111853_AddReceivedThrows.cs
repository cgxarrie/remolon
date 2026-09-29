using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedThrows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedThrows",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ObjectId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    LastThrownAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedThrows", x => new { x.UserId, x.ObjectId });
                    table.ForeignKey(
                        name: "FK_ReceivedThrows_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedThrows");
        }
    }
}
