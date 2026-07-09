namespace StockTrace.Api.Contracts.Transfers;

public sealed record CreateStockTransferRequest(
    string TransferNumber,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    DateTimeOffset TransferredAt,
    IReadOnlyCollection<CreateStockTransferLineRequest> Lines);

public sealed record CreateStockTransferLineRequest(Guid ProductId, decimal Quantity);
