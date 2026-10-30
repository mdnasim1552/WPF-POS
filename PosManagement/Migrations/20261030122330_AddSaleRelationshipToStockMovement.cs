using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleRelationshipToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SaleId",
                table: "StockMovements",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Sales_SaleId",
                table: "StockMovements",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "SaleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Sales_SaleId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SaleId",
                table: "StockMovements");
        }
    }
}
