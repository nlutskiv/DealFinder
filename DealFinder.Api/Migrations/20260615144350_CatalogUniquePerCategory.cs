using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class CatalogUniquePerCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductCatalog_SearchQuery",
                table: "ProductCatalog");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalog_Category_SearchQuery",
                table: "ProductCatalog",
                columns: new[] { "Category", "SearchQuery" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductCatalog_Category_SearchQuery",
                table: "ProductCatalog");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalog_SearchQuery",
                table: "ProductCatalog",
                column: "SearchQuery",
                unique: true);
        }
    }
}
