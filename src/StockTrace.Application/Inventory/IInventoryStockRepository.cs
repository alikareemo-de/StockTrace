using StockTrace.Domain.Inventory;

namespace StockTrace.Application.Inventory;

public interface IInventoryStockRepository
{
    Task<IReadOnlyList<InventoryBalance>> GetAvailableBalancesForUpdateAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<InventoryBalance?> GetBalanceForUpdateAsync(
        Guid warehouseId,
        Guid inventoryLotId,
        CancellationToken cancellationToken);
}
