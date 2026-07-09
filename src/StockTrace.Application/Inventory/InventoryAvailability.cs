using StockTrace.Application.Common.Exceptions;

namespace StockTrace.Application.Inventory;

public sealed record InventoryAvailabilityResult(
    Guid WarehouseId,
    Guid ProductId,
    decimal QuantityOnHand,
    IReadOnlyCollection<AvailableLotResult> Lots);

public sealed record AvailableLotResult(
    Guid InventoryLotId,
    string LotNumber,
    Guid SupplierId,
    DateTimeOffset ReceivedAt,
    decimal QuantityOnHand,
    decimal UnitCost);

public interface IInventoryAvailabilityRepository
{
    Task<InventoryAvailabilityResult> GetAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken);
}

public interface IInventoryQueryService
{
    Task<InventoryAvailabilityResult> GetAvailabilityAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken);
}

internal sealed class InventoryQueryService(IInventoryAvailabilityRepository repository) : IInventoryQueryService
{
    public Task<InventoryAvailabilityResult> GetAvailabilityAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (warehouseId == Guid.Empty) errors["warehouseId"] = ["Warehouse is required."];
        if (productId == Guid.Empty) errors["productId"] = ["Product is required."];
        if (errors.Count > 0) throw new ValidationException(errors);

        return repository.GetAsync(warehouseId, productId, cancellationToken);
    }
}
