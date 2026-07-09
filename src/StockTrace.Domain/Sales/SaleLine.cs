using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;
using StockTrace.Domain.Inventory;

namespace StockTrace.Domain.Sales;

public sealed class SaleLine : Entity
{
    private readonly List<SaleLotAllocation> _allocations = [];
    private SaleLine() { }

    public SaleLine(Guid saleId, Guid productId, decimal quantity, decimal unitPrice) : base(Guid.NewGuid())
    {
        SaleId = saleId;
        ProductId = productId;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
    }

    public Guid SaleId { get; private set; }
    public Sale Sale { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public IReadOnlyCollection<SaleLotAllocation> Allocations => _allocations;

    public SaleLotAllocation AddAllocation(InventoryLot inventoryLot, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(inventoryLot);
        if (_allocations.Sum(x => x.Quantity) + quantity > Quantity)
            throw new InvalidOperationException("Allocated quantity cannot exceed the sale-line quantity.");

        var allocation = new SaleLotAllocation(Id, inventoryLot, quantity);
        _allocations.Add(allocation);
        return allocation;
    }
}
