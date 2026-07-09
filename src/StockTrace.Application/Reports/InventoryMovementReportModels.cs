namespace StockTrace.Application.Reports;

public sealed record InventoryMovementReportQuery(
    Guid? WarehouseId,
    Guid? SupplierId,
    Guid? CategoryId,
    Guid? ProductId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber = 1,
    int PageSize = 50);

public sealed record InventoryMovementReportItem(
    Guid MovementId,
    DateTimeOffset OccurredAt,
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid CategoryId,
    string CategoryName,
    Guid SupplierId,
    string SupplierName,
    Guid InventoryLotId,
    string LotNumber,
    string MovementType,
    decimal Quantity,
    decimal UnitCost,
    decimal Value,
    string ReferenceType,
    Guid ReferenceId);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
