using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiEvaluationCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    UrlHash = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    ConditionScore = table.Column<double>(type: "double precision", nullable: false),
                    SellerTrustScore = table.Column<double>(type: "double precision", nullable: false),
                    RiskScore = table.Column<double>(type: "double precision", nullable: false),
                    RiskLabel = table.Column<string>(type: "text", nullable: false),
                    DetectedIssuesJson = table.Column<string>(type: "text", nullable: false),
                    PositiveSignalsJson = table.Column<string>(type: "text", nullable: false),
                    AnalysisSummary = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    SearchQuery = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    SellerName = table.Column<string>(type: "text", nullable: false),
                    SellerFeedbackPercentage = table.Column<double>(type: "double precision", nullable: true),
                    SellerFeedbackScore = table.Column<int>(type: "integer", nullable: true),
                    Marketplace = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    ProductCatalogId = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductListings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationCaches_UrlHash",
                table: "AiEvaluationCaches",
                column: "UrlHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalog_SearchQuery",
                table: "ProductCatalog",
                column: "SearchQuery",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductListings_ProductCatalogId",
                table: "ProductListings",
                column: "ProductCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductListings_Url",
                table: "ProductListings",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEvaluationCaches");

            migrationBuilder.DropTable(
                name: "ProductCatalog");

            migrationBuilder.DropTable(
                name: "ProductListings");
        }
    }
}
