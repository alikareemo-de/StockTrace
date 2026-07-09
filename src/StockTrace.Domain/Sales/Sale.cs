using StockTrace.Domain.Common;
using StockTrace.Domain.Warehousing;

namespace StockTrace.Domain.Sales;

public enum SaleStatus { Draft = 1, Posted = 2, Cancelled = 3 }

public sealed class Sale : AuditableEntity
{
    private readonly List<SaleLine> _lines = [];
    private Sale() { }

    public Sale(string saleNumber, Guid warehouseId, DateTimeOffset soldAt) : base(Guid.NewGuid())
    {
        SaleNumber = Guard.Required(saleNumber, nameof(saleNumber), 50);
        WarehouseId = warehouseId;
        SoldAt = soldAt;
        Status = SaleStatus.Draft;
    }

    public string SaleNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public DateTimeOffset SoldAt { get; private set; }
    public SaleStatus Status { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;

    public SaleLine AddLine(Guid productId, decimal quantity, decimal unitPrice)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to a draft sale.");

        var line = new SaleLine(Id, productId, quantity, unitPrice);
        _lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Only a draft sale can be posted.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("A sale must contain at least one line before posting.");
        if (_lines.Any(x => x.Allocations.Sum(a => a.Quantity) != x.Quantity))
            throw new InvalidOperationException("Every sale line must be fully allocated before posting.");

        Status = SaleStatus.Posted;
    }
}
