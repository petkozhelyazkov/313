using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiUsageLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Endpoint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbols = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    QuotaUsedToday = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsageLog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoricalPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalPrices", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PriceCache",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DayChange = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DayChangePct = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    PreviousClose = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsStale = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceCache", x => x.Symbol);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Exchange = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstrumentType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    LastMetadataRefreshAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLog_RequestedAt",
                table: "ApiUsageLog",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_Symbol_Date",
                table: "HistoricalPrices",
                columns: new[] { "Symbol", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Name",
                table: "Stocks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Symbol",
                table: "Stocks",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiUsageLog");

            migrationBuilder.DropTable(
                name: "HistoricalPrices");

            migrationBuilder.DropTable(
                name: "PriceCache");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
