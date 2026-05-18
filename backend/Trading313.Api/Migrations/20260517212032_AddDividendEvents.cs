using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDividendEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DividendEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DividendEvents_ExDate",
                table: "DividendEvents",
                column: "ExDate");

            migrationBuilder.CreateIndex(
                name: "IX_DividendEvents_Symbol_ExDate",
                table: "DividendEvents",
                columns: new[] { "Symbol", "ExDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DividendEvents");
        }
    }
}
