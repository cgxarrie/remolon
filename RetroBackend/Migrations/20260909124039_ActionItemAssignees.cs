using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroBackend.Migrations
{
    /// <inheritdoc />
    public partial class ActionItemAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Assignees",
                table: "Items",
                type: "text[]",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Items"
                SET "Assignees" = CASE
                    WHEN "Assignee" IS NULL OR btrim("Assignee") = '' THEN ARRAY[]::text[]
                    ELSE ARRAY[btrim("Assignee")]
                END
                WHERE "ItemType" = 'ActionItem';

                UPDATE "Items"
                SET "Assignees" = ARRAY[]::text[]
                WHERE "Assignees" IS NULL;
                """);

            migrationBuilder.AlterColumn<string[]>(
                name: "Assignees",
                table: "Items",
                type: "text[]",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "Assignee",
                table: "Items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assignee",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Items"
                SET "Assignee" = CASE
                    WHEN "Assignees" IS NULL OR cardinality("Assignees") = 0 THEN ''
                    ELSE "Assignees"[1]
                END
                WHERE "ItemType" = 'ActionItem';
                """);

            migrationBuilder.DropColumn(
                name: "Assignees",
                table: "Items");
        }
    }
}
