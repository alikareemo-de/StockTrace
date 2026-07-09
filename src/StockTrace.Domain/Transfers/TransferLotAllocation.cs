using StockTrace.Domain.Common;
using StockTrace.Domain.Inventory;

namespace StockTrace.Domain.Transfers;

public sealed class TransferLotAllocation : Entity
{
    private TransferLotAllocation() { }

    public TransferLotAllocation(Guid stockTransferLineId, InventoryLot inventoryLot, decimal quantity) : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNull(inventoryLot);
        StockTransferLineId = stockTransferLineId;
        InventoryLotId = inventoryLot.Id;
        InventoryLot = inventoryLot;
        Quantity = Guard.Positive(quantity, nameof(quantity));
    }

    public Guid StockTransferLineId { get; private set; }
    public StockTransferLine StockTransferLine { get; private set; } = null!;
    public Guid InventoryLotId { get; private set; }
    public InventoryLot InventoryLot { get; private set; } = null!;
    public decimal Quantity { get; private set; }
}
