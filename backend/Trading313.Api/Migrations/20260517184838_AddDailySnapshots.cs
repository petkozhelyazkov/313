using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyPortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CashBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    HoldingsValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalInvestedAtSnapshot = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnrealizedPl = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPortfolioSnapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPortfolioSnapshots_UserId_SnapshotDate",
                table: "DailyPortfolioSnapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyPortfolioSnapshots");
        }
    }
}
