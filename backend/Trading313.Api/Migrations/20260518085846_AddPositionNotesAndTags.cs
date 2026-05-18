using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionNotesAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Positions",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Positions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Positions");
        }
    }
}
