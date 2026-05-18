using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Beta",
                table: "Stocks",
                type: "decimal(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ceo",
                table: "Stocks",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Stocks",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DividendYield",
                table: "Stocks",
                type: "decimal(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Employees",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Eps",
                table: "Stocks",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FiftyTwoWeekHigh",
                table: "Stocks",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FiftyTwoWeekLow",
                table: "Stocks",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Stocks",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "Stocks",
                type: "decimal(28,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PeRatio",
                table: "Stocks",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Stocks",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Stocks",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Sector",
                table: "Stocks",
                column: "Sector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_Sector",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Beta",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Ceo",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DividendYield",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Employees",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Eps",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "FiftyTwoWeekHigh",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "FiftyTwoWeekLow",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "PeRatio",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Stocks");
        }
    }
}
