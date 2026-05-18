using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId_IsCompleted",
                table: "Goals",
                columns: new[] { "UserId", "IsCompleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Goals");
        }
    }
}
