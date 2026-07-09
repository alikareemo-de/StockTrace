namespace StockTrace.Application.Inventory;

public sealed record LowStockCheck(
    Guid WarehouseId,
    Guid ProductId,
    decimal QuantityBefore,
    decimal QuantityAfter,
    DateTimeOffset OccurredAt,
    string TriggeredBy);

public sealed record LowStockAlert(
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Threshold,
    decimal QuantityOnHand,
    DateTimeOffset OccurredAt,
    string TriggeredBy);

public sealed record StockChangedAlert(
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityBefore,
    decimal QuantityAfter,
    DateTimeOffset OccurredAt,
    string TriggeredBy);

public interface IInventoryRealtimeNotificationService
{
    Task PublishAsync(
        IReadOnlyCollection<LowStockCheck> checks,
        CancellationToken cancellationToken);
}
