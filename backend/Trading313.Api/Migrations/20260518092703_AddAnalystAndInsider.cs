using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalystAndInsider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalystRatings",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NumAnalysts = table.Column<int>(type: "int", nullable: false),
                    RecommendationMean = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    StrongBuy = table.Column<int>(type: "int", nullable: false),
                    Buy = table.Column<int>(type: "int", nullable: false),
                    Hold = table.Column<int>(type: "int", nullable: false),
                    Sell = table.Column<int>(type: "int", nullable: false),
                    StrongSell = table.Column<int>(type: "int", nullable: false),
                    TargetLow = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TargetMean = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TargetHigh = table.Column<decimal>(type: "decimal(18,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystRatings", x => x.Symbol);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InsiderTrades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PersonName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TransactionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Shares = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PricePerShare = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsiderTrades", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InsiderTrades_Symbol_TransactionDate",
                table: "InsiderTrades",
                columns: new[] { "Symbol", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalystRatings");

            migrationBuilder.DropTable(
                name: "InsiderTrades");
        }
    }
}
