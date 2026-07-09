using StockTrace.Domain.Common;
using StockTrace.Domain.Partners;
using StockTrace.Domain.Warehousing;

namespace StockTrace.Domain.Purchasing;

public enum PurchaseReceiptStatus { Draft = 1, Posted = 2, Cancelled = 3 }

public sealed class PurchaseReceipt : AuditableEntity
{
    private readonly List<PurchaseReceiptLine> _lines = [];
    private PurchaseReceipt() { }

    public PurchaseReceipt(string receiptNumber, Guid supplierId, Guid warehouseId, DateTimeOffset receivedAt)
        : base(Guid.NewGuid())
    {
        ReceiptNumber = Guard.Required(receiptNumber, nameof(receiptNumber), 50);
        SupplierId = supplierId;
        WarehouseId = warehouseId;
        ReceivedAt = receivedAt;
        Status = PurchaseReceiptStatus.Draft;
    }

    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public PurchaseReceiptStatus Status { get; private set; }
    public IReadOnlyCollection<PurchaseReceiptLine> Lines => _lines;

    public PurchaseReceiptLine AddLine(Guid productId, decimal quantity, decimal unitCost)
    {
        if (Status != PurchaseReceiptStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to a draft receipt.");

        var line = new PurchaseReceiptLine(Id, productId, quantity, unitCost);
        _lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != PurchaseReceiptStatus.Draft)
            throw new InvalidOperationException("Only a draft receipt can be posted.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("A receipt must contain at least one line before posting.");

        Status = PurchaseReceiptStatus.Posted;
    }
}
