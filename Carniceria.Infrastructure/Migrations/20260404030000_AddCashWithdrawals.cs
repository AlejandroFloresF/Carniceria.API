using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carniceria.Infrastructure.Migrations
{
    public partial class AddCashWithdrawals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashWithdrawals",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId   = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount      = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note        = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_CashWithdrawals", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_CashWithdrawals_SessionId",
                table: "CashWithdrawals",
                column: "SessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CashWithdrawals");
        }
    }
}
