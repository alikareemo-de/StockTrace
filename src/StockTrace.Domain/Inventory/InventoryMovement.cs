using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;
using StockTrace.Domain.Warehousing;

namespace StockTrace.Domain.Inventory;

public enum InventoryMovementType { PurchaseReceipt = 1, SaleIssue = 2, TransferOut = 3, TransferIn = 4, Adjustment = 5 }

public sealed class InventoryMovement : AuditableEntity
{
    private InventoryMovement() { }

    public InventoryMovement(Guid warehouseId, Guid productId, Guid inventoryLotId,
        InventoryMovementType movementType, decimal quantity, decimal unitCost,
        DateTimeOffset occurredAt, string referenceType, Guid referenceId, Guid correlationId) : base(Guid.NewGuid())
    {
        if (quantity == 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be zero.");
        WarehouseId = warehouseId;
        ProductId = productId;
        InventoryLotId = inventoryLotId;
        MovementType = movementType;
        Quantity = quantity;
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
        OccurredAt = occurredAt;
        ReferenceType = Guard.Required(referenceType, nameof(referenceType), 50);
        ReferenceId = referenceId;
        CorrelationId = correlationId;
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid InventoryLotId { get; private set; }
    public InventoryLot InventoryLot { get; private set; } = null!;
    public InventoryMovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public Guid CorrelationId { get; private set; }
}
