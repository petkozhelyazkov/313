using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TriggerPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TriggeredPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Acknowledged = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_Status_Symbol",
                table: "PriceAlerts",
                columns: new[] { "Status", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId_Status",
                table: "PriceAlerts",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAlerts");
        }
    }
}
