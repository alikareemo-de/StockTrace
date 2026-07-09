using StockTrace.Domain.Common;
using StockTrace.Domain.Warehousing;

namespace StockTrace.Domain.Transfers;

public enum StockTransferStatus { Draft = 1, Completed = 2, Cancelled = 3 }

public sealed class StockTransfer : AuditableEntity
{
    private readonly List<StockTransferLine> _lines = [];
    private StockTransfer() { }

    public StockTransfer(string transferNumber, Guid sourceWarehouseId, Guid destinationWarehouseId) : base(Guid.NewGuid())
    {
        if (sourceWarehouseId == destinationWarehouseId)
            throw new ArgumentException("Source and destination warehouses must be different.", nameof(destinationWarehouseId));

        TransferNumber = Guard.Required(transferNumber, nameof(transferNumber), 50);
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        Status = StockTransferStatus.Draft;
    }

    public string TransferNumber { get; private set; } = string.Empty;
    public Guid SourceWarehouseId { get; private set; }
    public Warehouse SourceWarehouse { get; private set; } = null!;
    public Guid DestinationWarehouseId { get; private set; }
    public Warehouse DestinationWarehouse { get; private set; } = null!;
    public StockTransferStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public IReadOnlyCollection<StockTransferLine> Lines => _lines;

    public StockTransferLine AddLine(Guid productId, decimal requestedQuantity)
    {
        if (Status != StockTransferStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to a draft transfer.");

        var line = new StockTransferLine(Id, productId, requestedQuantity);
        _lines.Add(line);
        return line;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status != StockTransferStatus.Draft)
            throw new InvalidOperationException("Only a draft transfer can be completed.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("A transfer must contain at least one line.");
        if (_lines.Any(x => x.Allocations.Sum(a => a.Quantity) != x.RequestedQuantity))
            throw new InvalidOperationException("Every transfer line must be fully allocated before completion.");

        Status = StockTransferStatus.Completed;
        CompletedAt = completedAt;
    }
}
