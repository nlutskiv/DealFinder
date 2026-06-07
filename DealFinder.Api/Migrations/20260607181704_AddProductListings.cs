using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    SellerName = table.Column<string>(type: "TEXT", nullable: false),
                    SellerFeedbackPercentage = table.Column<double>(type: "REAL", nullable: true),
                    SellerFeedbackScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Marketplace = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductListings", x => x.Id);
                });

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
                name: "ProductListings");
        }
    }
}
