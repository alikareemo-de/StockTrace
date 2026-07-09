namespace StockTrace.Api.Contracts.Purchases;

public sealed record ReceivePurchaseRequest(
    string ReceiptNumber,
    Guid SupplierId,
    Guid WarehouseId,
    DateTimeOffset ReceivedAt,
    IReadOnlyCollection<ReceivePurchaseLineRequest> Lines);

public sealed record ReceivePurchaseLineRequest(Guid ProductId, decimal Quantity, decimal UnitCost);
