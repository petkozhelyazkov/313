using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchlistListName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_UserId_Symbol",
                table: "WatchlistItems");

            migrationBuilder.AddColumn<string>(
                name: "ListName",
                table: "WatchlistItems",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Default")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId_ListName",
                table: "WatchlistItems",
                columns: new[] { "UserId", "ListName" });

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId_ListName_Symbol",
                table: "WatchlistItems",
                columns: new[] { "UserId", "ListName", "Symbol" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_UserId_ListName",
                table: "WatchlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_UserId_ListName_Symbol",
                table: "WatchlistItems");

            migrationBuilder.DropColumn(
                name: "ListName",
                table: "WatchlistItems");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId_Symbol",
                table: "WatchlistItems",
                columns: new[] { "UserId", "Symbol" },
                unique: true);
        }
    }
}
