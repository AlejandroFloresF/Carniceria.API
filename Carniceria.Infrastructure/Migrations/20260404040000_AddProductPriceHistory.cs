using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carniceria.Infrastructure.Migrations
{
    public partial class AddProductPriceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPriceHistory",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId   = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldPrice    = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewPrice    = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_ProductPriceHistory", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_ProductId",
                table: "ProductPriceHistory",
                column: "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductPriceHistory");
        }
    }
}
