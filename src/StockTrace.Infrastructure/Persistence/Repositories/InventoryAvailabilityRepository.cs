using StockTrace.Application.Inventory;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class InventoryAvailabilityRepository(ApplicationDbContext dbContext)
    : IInventoryAvailabilityRepository
{
    public async Task<InventoryAvailabilityResult> GetAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var lots = await dbContext.InventoryBalances.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId &&
                        x.InventoryLot.ProductId == productId &&
                        x.QuantityOnHand > 0)
            .OrderBy(x => x.InventoryLot.ReceivedAt)
            .ThenBy(x => x.InventoryLotId)
            .Select(x => new AvailableLotResult(
                x.InventoryLotId,
                x.InventoryLot.LotNumber,
                x.InventoryLot.SupplierId,
                x.InventoryLot.ReceivedAt,
                x.QuantityOnHand,
                x.InventoryLot.UnitCost))
            .ToArrayAsync(cancellationToken);

        return new InventoryAvailabilityResult(
            warehouseId, productId, lots.Sum(x => x.QuantityOnHand), lots);
    }
}
