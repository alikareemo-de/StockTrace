namespace StockTrace.Application.Transfers;

public sealed record CreateStockTransferCommand(
    string TransferNumber,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    DateTimeOffset TransferredAt,
    IReadOnlyCollection<CreateStockTransferLine> Lines);

public sealed record CreateStockTransferLine(Guid ProductId, decimal Quantity);

public sealed record StockTransferResult(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    string Status,
    DateTimeOffset? CompletedAt,
    IReadOnlyCollection<StockTransferLineResult> Lines);

public sealed record StockTransferLineResult(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    IReadOnlyCollection<TransferAllocationResult> Allocations);

public sealed record TransferAllocationResult(
    Guid InventoryLotId,
    string LotNumber,
    Guid SupplierId,
    decimal Quantity,
    decimal UnitCost);
