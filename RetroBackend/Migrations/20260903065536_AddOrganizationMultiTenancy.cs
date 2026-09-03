using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Retrospectives",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Retrospectives") THEN
                        INSERT INTO "Organizations" ("Id", "Name")
                        VALUES ('00000000-0000-0000-0000-000000000001', 'Default');
                        UPDATE "Retrospectives"
                        SET "OrganizationId" = '00000000-0000-0000-0000-000000000001'
                        WHERE "OrganizationId" IS NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Retrospectives",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Retrospectives_OrganizationId",
                table: "Retrospectives",
                column: "OrganizationId");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_AspNetUsers_Nickname_CaseInsensitive"
                ON "AspNetUsers" (LOWER("Nickname"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_Organizations_Name_CaseInsensitive"
                ON "Organizations" (LOWER("Name"));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Retrospectives_Organizations_OrganizationId",
                table: "Retrospectives",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Retrospectives_Organizations_OrganizationId",
                table: "Retrospectives");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Retrospectives_OrganizationId",
                table: "Retrospectives");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Nickname_CaseInsensitive",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Retrospectives");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AspNetUsers");
        }
    }
}
