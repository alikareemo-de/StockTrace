using StockTrace.Domain.Common;
using StockTrace.Domain.Inventory;

namespace StockTrace.Domain.Sales;

public sealed class SaleLotAllocation : Entity
{
    private SaleLotAllocation() { }

    public SaleLotAllocation(Guid saleLineId, InventoryLot inventoryLot, decimal quantity) : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNull(inventoryLot);
        SaleLineId = saleLineId;
        InventoryLotId = inventoryLot.Id;
        InventoryLot = inventoryLot;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = inventoryLot.UnitCost;
    }

    public Guid SaleLineId { get; private set; }
    public SaleLine SaleLine { get; private set; } = null!;
    public Guid InventoryLotId { get; private set; }
    public InventoryLot InventoryLot { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
}
