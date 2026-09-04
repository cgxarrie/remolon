using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTheming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemeAccentColor",
                table: "Organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeAccentHoverColor",
                table: "Organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeFocusColor",
                table: "Organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeHeaderColor",
                table: "Organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeHeaderHoverColor",
                table: "Organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeKey",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.Sql("""
                UPDATE "Organizations" SET "ThemeKey" = 'ocean'
                WHERE LOWER("Name") = 'globex';
                UPDATE "Organizations" SET "ThemeKey" = 'forest'
                WHERE LOWER("Name") = 'initech';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemeAccentColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ThemeAccentHoverColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ThemeFocusColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ThemeHeaderColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ThemeHeaderHoverColor",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ThemeKey",
                table: "Organizations");
        }
    }
}
