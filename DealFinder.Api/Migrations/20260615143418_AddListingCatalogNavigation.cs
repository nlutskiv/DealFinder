using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddListingCatalogNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ProductListings_ProductCatalog_ProductCatalogId",
                table: "ProductListings",
                column: "ProductCatalogId",
                principalTable: "ProductCatalog",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductListings_ProductCatalog_ProductCatalogId",
                table: "ProductListings");
        }
    }
}
