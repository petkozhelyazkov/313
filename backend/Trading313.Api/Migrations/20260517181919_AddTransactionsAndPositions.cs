using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionsAndPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RealizedPlLifetime = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    FirstPurchasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastTransactionAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsClosed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    PricePerShare = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Fees = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RealizedPl = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId_Symbol",
                table: "Positions",
                columns: new[] { "UserId", "Symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_ExecutedAt",
                table: "Transactions",
                columns: new[] { "UserId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_Symbol",
                table: "Transactions",
                columns: new[] { "UserId", "Symbol" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
