using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;
using StockTrace.Domain.Partners;
using StockTrace.Domain.Purchasing;

namespace StockTrace.Domain.Inventory;

public sealed class InventoryLot : AuditableEntity
{
    private InventoryLot() { }

    public InventoryLot(string lotNumber, Guid productId, Guid supplierId, Guid purchaseReceiptLineId,
        DateTimeOffset receivedAt, decimal originalQuantity, decimal unitCost) : base(Guid.NewGuid())
    {
        LotNumber = Guard.Required(lotNumber, nameof(lotNumber), 80);
        ProductId = productId;
        SupplierId = supplierId;
        PurchaseReceiptLineId = purchaseReceiptLineId;
        ReceivedAt = receivedAt;
        OriginalQuantity = Guard.Positive(originalQuantity, nameof(originalQuantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
    }

    public string LotNumber { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public Guid PurchaseReceiptLineId { get; private set; }
    public PurchaseReceiptLine PurchaseReceiptLine { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public decimal OriginalQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
}
