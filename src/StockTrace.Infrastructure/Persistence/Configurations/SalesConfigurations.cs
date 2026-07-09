using StockTrace.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockTrace.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.Property(x => x.SaleNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.SaleNumber).IsUnique();
        builder.HasIndex(x => new { x.WarehouseId, x.SoldAt });
        builder.HasMany(x => x.Lines).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines", table =>
        {
            table.HasCheckConstraint("CK_SaleLines_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_SaleLines_UnitPrice_NotNegative", "[UnitPrice] >= 0");
        });
        builder.HasIndex(x => new { x.SaleId, x.ProductId });
        builder.HasMany(x => x.Allocations).WithOne(x => x.SaleLine).HasForeignKey(x => x.SaleLineId);
        builder.Navigation(x => x.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SaleLotAllocationConfiguration : IEntityTypeConfiguration<SaleLotAllocation>
{
    public void Configure(EntityTypeBuilder<SaleLotAllocation> builder)
    {
        builder.ToTable("SaleLotAllocations", table =>
        {
            table.HasCheckConstraint("CK_SaleLotAllocations_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_SaleLotAllocations_UnitCost_NotNegative", "[UnitCost] >= 0");
        });
        builder.HasIndex(x => x.InventoryLotId);
        builder.HasIndex(x => new { x.SaleLineId, x.InventoryLotId }).IsUnique();
    }
}
