using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trading313.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDigest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailDigestEnabled",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "EmailDigests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PeriodStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Subject = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyHtml = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDigests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDigests_UserId_GeneratedAt",
                table: "EmailDigests",
                columns: new[] { "UserId", "GeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDigests");

            migrationBuilder.DropColumn(
                name: "EmailDigestEnabled",
                table: "AspNetUsers");
        }
    }
}
