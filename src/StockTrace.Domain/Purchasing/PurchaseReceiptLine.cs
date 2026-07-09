using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;

namespace StockTrace.Domain.Purchasing;

public sealed class PurchaseReceiptLine : Entity
{
    private PurchaseReceiptLine() { }

    public PurchaseReceiptLine(Guid purchaseReceiptId, Guid productId, decimal quantity, decimal unitCost)
        : base(Guid.NewGuid())
    {
        PurchaseReceiptId = purchaseReceiptId;
        ProductId = productId;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
    }

    public Guid PurchaseReceiptId { get; private set; }
    public PurchaseReceipt PurchaseReceipt { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
}
