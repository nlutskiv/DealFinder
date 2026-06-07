using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEvaluationCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiEvaluationCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    UrlHash = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationCaches_UrlHash",
                table: "AiEvaluationCaches",
                column: "UrlHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEvaluationCaches");
        }
    }
}
