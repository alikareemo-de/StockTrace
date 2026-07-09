using StockTrace.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockTrace.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ToTable("PurchaseReceipts");
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => new { x.SupplierId, x.ReceivedAt });
        builder.HasMany(x => x.Lines).WithOne(x => x.PurchaseReceipt).HasForeignKey(x => x.PurchaseReceiptId);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PurchaseReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptLine> builder)
    {
        builder.ToTable("PurchaseReceiptLines", table =>
        {
            table.HasCheckConstraint("CK_PurchaseReceiptLines_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_PurchaseReceiptLines_UnitCost_NotNegative", "[UnitCost] >= 0");
        });
        builder.HasIndex(x => new { x.PurchaseReceiptId, x.ProductId });
    }
}
