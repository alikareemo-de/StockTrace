namespace StockTrace.Application.Purchases;

public sealed record ReceivePurchaseCommand(
    string ReceiptNumber,
    Guid SupplierId,
    Guid WarehouseId,
    DateTimeOffset ReceivedAt,
    IReadOnlyCollection<ReceivePurchaseLine> Lines);

public sealed record ReceivePurchaseLine(Guid ProductId, decimal Quantity, decimal UnitCost);

public sealed record PurchaseReceiptResult(
    Guid Id,
    string ReceiptNumber,
    Guid SupplierId,
    Guid WarehouseId,
    DateTimeOffset ReceivedAt,
    string Status,
    IReadOnlyCollection<PurchaseReceiptLineResult> Lines);

public sealed record PurchaseReceiptLineResult(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    Guid InventoryLotId,
    string LotNumber);
