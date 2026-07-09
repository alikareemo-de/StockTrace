namespace StockTrace.Api.Contracts.Sales;

public sealed record CreateSaleRequest(
    string SaleNumber,
    Guid WarehouseId,
    DateTimeOffset SoldAt,
    IReadOnlyCollection<CreateSaleLineRequest> Lines);

public sealed record CreateSaleLineRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);
