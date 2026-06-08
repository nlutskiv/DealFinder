using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSearchFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductCatalogId",
                table: "ProductListings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    SearchQuery = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductListings_ProductCatalogId",
                table: "ProductListings",
                column: "ProductCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalog_SearchQuery",
                table: "ProductCatalog",
                column: "SearchQuery",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCatalog");

            migrationBuilder.DropIndex(
                name: "IX_ProductListings_ProductCatalogId",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "ProductCatalogId",
                table: "ProductListings");
        }
    }
}
