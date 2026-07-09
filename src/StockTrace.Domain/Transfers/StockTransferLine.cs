using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;
using StockTrace.Domain.Inventory;

namespace StockTrace.Domain.Transfers;

public sealed class StockTransferLine : Entity
{
    private readonly List<TransferLotAllocation> _allocations = [];
    private StockTransferLine() { }

    public StockTransferLine(Guid stockTransferId, Guid productId, decimal requestedQuantity) : base(Guid.NewGuid())
    {
        StockTransferId = stockTransferId;
        ProductId = productId;
        RequestedQuantity = Guard.Positive(requestedQuantity, nameof(requestedQuantity));
    }

    public Guid StockTransferId { get; private set; }
    public StockTransfer StockTransfer { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public decimal RequestedQuantity { get; private set; }
    public IReadOnlyCollection<TransferLotAllocation> Allocations => _allocations;

    public TransferLotAllocation AddAllocation(InventoryLot inventoryLot, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(inventoryLot);
        if (_allocations.Sum(x => x.Quantity) + quantity > RequestedQuantity)
            throw new InvalidOperationException("Allocated quantity cannot exceed the requested quantity.");

        var allocation = new TransferLotAllocation(Id, inventoryLot, quantity);
        _allocations.Add(allocation);
        return allocation;
    }
}
