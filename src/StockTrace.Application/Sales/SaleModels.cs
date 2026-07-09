namespace StockTrace.Application.Sales;

public sealed record CreateSaleCommand(
    string SaleNumber,
    Guid WarehouseId,
    DateTimeOffset SoldAt,
    IReadOnlyCollection<CreateSaleLine> Lines);

public sealed record CreateSaleLine(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record SaleResult(
    Guid Id,
    string SaleNumber,
    Guid WarehouseId,
    DateTimeOffset SoldAt,
    string Status,
    IReadOnlyCollection<SaleLineResult> Lines);

public sealed record SaleLineResult(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal CostOfGoodsSold,
    IReadOnlyCollection<SaleAllocationResult> Allocations);

public sealed record SaleAllocationResult(
    Guid InventoryLotId,
    string LotNumber,
    Guid SupplierId,
    decimal Quantity,
    decimal UnitCost);
