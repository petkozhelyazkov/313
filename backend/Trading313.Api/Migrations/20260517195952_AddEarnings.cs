using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EarningsEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Time = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EpsEstimate = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    EpsActual = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    SurprisePercent = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningsEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EarningsEntries_ReportDate",
                table: "EarningsEntries",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_EarningsEntries_Symbol_ReportDate",
                table: "EarningsEntries",
                columns: new[] { "Symbol", "ReportDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EarningsEntries");
        }
    }
}
