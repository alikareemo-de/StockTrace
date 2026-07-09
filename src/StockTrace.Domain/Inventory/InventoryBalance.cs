using StockTrace.Domain.Common;
using StockTrace.Domain.Warehousing;

namespace StockTrace.Domain.Inventory;

public sealed class InventoryBalance : AuditableEntity
{
    private InventoryBalance() { }

    public InventoryBalance(Guid warehouseId, Guid inventoryLotId, decimal quantityOnHand) : base(Guid.NewGuid())
    {
        WarehouseId = warehouseId;
        InventoryLotId = inventoryLotId;
        QuantityOnHand = Guard.NotNegative(quantityOnHand, nameof(quantityOnHand));
    }

    public InventoryBalance(Guid warehouseId, InventoryLot inventoryLot, decimal quantityOnHand)
        : this(warehouseId, inventoryLot?.Id ?? throw new ArgumentNullException(nameof(inventoryLot)), quantityOnHand)
    {
        InventoryLot = inventoryLot;
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public Guid InventoryLotId { get; private set; }
    public InventoryLot InventoryLot { get; private set; } = null!;
    public decimal QuantityOnHand { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Decrease(decimal quantity)
    {
        Guard.Positive(quantity, nameof(quantity));
        if (QuantityOnHand < quantity)
            throw new InvalidOperationException("Insufficient inventory balance.");

        QuantityOnHand -= quantity;
    }

    public void Increase(decimal quantity)
    {
        QuantityOnHand += Guard.Positive(quantity, nameof(quantity));
    }
}
