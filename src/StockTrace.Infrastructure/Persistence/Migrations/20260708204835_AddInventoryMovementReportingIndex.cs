using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryMovementReportingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OccurredAt_WarehouseId",
                table: "InventoryMovements",
                columns: new[] { "OccurredAt", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OccurredAt_WarehouseId",
                table: "InventoryMovements");
        }
    }
}
