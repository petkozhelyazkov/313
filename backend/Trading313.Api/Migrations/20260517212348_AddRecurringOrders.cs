using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CashAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SuccessfulRuns = table.Column<int>(type: "int", nullable: false),
                    FailedRuns = table.Column<int>(type: "int", nullable: false),
                    LastFailureReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringOrders", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOrders_IsActive_NextRunAt",
                table: "RecurringOrders",
                columns: new[] { "IsActive", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringOrders_UserId_IsActive",
                table: "RecurringOrders",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringOrders");
        }
    }
}
