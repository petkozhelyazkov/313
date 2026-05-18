using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailingStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HighWaterMark",
                table: "PendingOrders",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrailingStopPercent",
                table: "PendingOrders",
                type: "decimal(8,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighWaterMark",
                table: "PendingOrders");

            migrationBuilder.DropColumn(
                name: "TrailingStopPercent",
                table: "PendingOrders");
        }
    }
}
