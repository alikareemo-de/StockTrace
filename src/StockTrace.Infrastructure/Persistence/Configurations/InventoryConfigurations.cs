using StockTrace.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockTrace.Infrastructure.Persistence.Configurations;

internal sealed class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable("InventoryLots", table =>
        {
            table.HasCheckConstraint("CK_InventoryLots_OriginalQuantity_Positive", "[OriginalQuantity] > 0");
            table.HasCheckConstraint("CK_InventoryLots_UnitCost_NotNegative", "[UnitCost] >= 0");
        });
        builder.Property(x => x.LotNumber).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.LotNumber).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.ReceivedAt, x.Id });
        builder.HasIndex(x => new { x.SupplierId, x.ProductId });
        builder.HasIndex(x => x.PurchaseReceiptLineId).IsUnique();
    }
}

internal sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances", table =>
            table.HasCheckConstraint("CK_InventoryBalances_QuantityOnHand_NotNegative", "[QuantityOnHand] >= 0"));
        builder.HasIndex(x => new { x.WarehouseId, x.InventoryLotId }).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

internal sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", table =>
        {
            table.HasCheckConstraint("CK_InventoryMovements_Quantity_NotZero", "[Quantity] <> 0");
            table.HasCheckConstraint("CK_InventoryMovements_UnitCost_NotNegative", "[UnitCost] >= 0");
        });
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ReferenceType).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId, x.OccurredAt });
        builder.HasIndex(x => new { x.OccurredAt, x.WarehouseId });
        builder.HasIndex(x => new { x.InventoryLotId, x.OccurredAt });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}
