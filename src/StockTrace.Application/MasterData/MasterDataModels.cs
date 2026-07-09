using StockTrace.Application.Common.Exceptions;

namespace StockTrace.Application.MasterData;

public sealed record CategoryResult(Guid Id, string Name);

public sealed record SupplierResult(Guid Id, string Code, string Name);

public sealed record WarehouseResult(Guid Id, string Code, string Name, string BranchName);

public sealed record ProductResult(
    Guid Id,
    string Sku,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string UnitOfMeasure,
    decimal DefaultLowStockThreshold,
    bool IsActive);

public sealed record WarehouseProductThresholdResult(
    Guid WarehouseId,
    Guid ProductId,
    decimal LowStockThreshold);

public interface IMasterDataRepository
{
    Task<IReadOnlyCollection<CategoryResult>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SupplierResult>> GetSuppliersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WarehouseResult>> GetWarehousesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductResult>> GetProductsAsync(CancellationToken cancellationToken);
    Task<WarehouseProductThresholdResult> UpsertLowStockThresholdAsync(
        Guid warehouseId,
        Guid productId,
        decimal lowStockThreshold,
        CancellationToken cancellationToken);
}

public interface IMasterDataService
{
    Task<IReadOnlyCollection<CategoryResult>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SupplierResult>> GetSuppliersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WarehouseResult>> GetWarehousesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductResult>> GetProductsAsync(CancellationToken cancellationToken);
    Task<WarehouseProductThresholdResult> SetLowStockThresholdAsync(
        Guid warehouseId,
        Guid productId,
        decimal lowStockThreshold,
        CancellationToken cancellationToken);
}

internal sealed class MasterDataService(IMasterDataRepository repository) : IMasterDataService
{
    public Task<IReadOnlyCollection<CategoryResult>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        repository.GetCategoriesAsync(cancellationToken);

    public Task<IReadOnlyCollection<SupplierResult>> GetSuppliersAsync(CancellationToken cancellationToken) =>
        repository.GetSuppliersAsync(cancellationToken);

    public Task<IReadOnlyCollection<WarehouseResult>> GetWarehousesAsync(CancellationToken cancellationToken) =>
        repository.GetWarehousesAsync(cancellationToken);

    public Task<IReadOnlyCollection<ProductResult>> GetProductsAsync(CancellationToken cancellationToken) =>
        repository.GetProductsAsync(cancellationToken);

    public Task<WarehouseProductThresholdResult> SetLowStockThresholdAsync(
        Guid warehouseId,
        Guid productId,
        decimal lowStockThreshold,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (warehouseId == Guid.Empty) errors["warehouseId"] = ["Warehouse is required."];
        if (productId == Guid.Empty) errors["productId"] = ["Product is required."];
        if (lowStockThreshold < 0) errors["lowStockThreshold"] = ["Low-stock threshold cannot be negative."];
        if (errors.Count > 0) throw new ValidationException(errors);

        return repository.UpsertLowStockThresholdAsync(
            warehouseId, productId, lowStockThreshold, cancellationToken);
    }
}
