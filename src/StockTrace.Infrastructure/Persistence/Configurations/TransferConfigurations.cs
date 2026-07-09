using StockTrace.Domain.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockTrace.Infrastructure.Persistence.Configurations;

internal sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", table =>
            table.HasCheckConstraint("CK_StockTransfers_DifferentWarehouses", "[SourceWarehouseId] <> [DestinationWarehouseId]"));
        builder.Property(x => x.TransferNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.TransferNumber).IsUnique();
        builder.HasOne(x => x.SourceWarehouse).WithMany().HasForeignKey(x => x.SourceWarehouseId);
        builder.HasOne(x => x.DestinationWarehouse).WithMany().HasForeignKey(x => x.DestinationWarehouseId);
        builder.HasMany(x => x.Lines).WithOne(x => x.StockTransfer).HasForeignKey(x => x.StockTransferId);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("StockTransferLines", table =>
            table.HasCheckConstraint("CK_StockTransferLines_RequestedQuantity_Positive", "[RequestedQuantity] > 0"));
        builder.HasIndex(x => new { x.StockTransferId, x.ProductId }).IsUnique();
        builder.HasMany(x => x.Allocations).WithOne(x => x.StockTransferLine).HasForeignKey(x => x.StockTransferLineId);
        builder.Navigation(x => x.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TransferLotAllocationConfiguration : IEntityTypeConfiguration<TransferLotAllocation>
{
    public void Configure(EntityTypeBuilder<TransferLotAllocation> builder)
    {
        builder.ToTable("TransferLotAllocations", table =>
            table.HasCheckConstraint("CK_TransferLotAllocations_Quantity_Positive", "[Quantity] > 0"));
        builder.HasIndex(x => new { x.StockTransferLineId, x.InventoryLotId }).IsUnique();
        builder.HasIndex(x => x.InventoryLotId);
    }
}
