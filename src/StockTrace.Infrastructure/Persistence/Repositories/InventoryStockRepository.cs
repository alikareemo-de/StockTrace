using StockTrace.Application.Inventory;
using StockTrace.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class InventoryStockRepository(ApplicationDbContext dbContext) : IInventoryStockRepository
{
    public async Task<IReadOnlyList<InventoryBalance>> GetAvailableBalancesForUpdateAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var balances = await dbContext.InventoryBalances
            .FromSqlInterpolated($$"""
                SELECT ib.*
                FROM InventoryBalances AS ib WITH (UPDLOCK, ROWLOCK)
                INNER JOIN InventoryLots AS il ON il.Id = ib.InventoryLotId
                WHERE ib.WarehouseId = {{warehouseId}}
                  AND il.ProductId = {{productId}}
                  AND ib.QuantityOnHand > 0
                """)
            .AsTracking()
            .ToListAsync(cancellationToken);

        var lotIds = balances.Select(x => x.InventoryLotId).ToArray();
        await dbContext.InventoryLots.Where(x => lotIds.Contains(x.Id)).LoadAsync(cancellationToken);

        return balances.OrderBy(x => x.InventoryLot.ReceivedAt).ThenBy(x => x.InventoryLot.Id).ToArray();
    }

    public Task<InventoryBalance?> GetBalanceForUpdateAsync(
        Guid warehouseId,
        Guid inventoryLotId,
        CancellationToken cancellationToken) =>
        dbContext.InventoryBalances
            .FromSqlInterpolated($$"""
                SELECT ib.*
                FROM InventoryBalances AS ib WITH (UPDLOCK, HOLDLOCK)
                WHERE ib.WarehouseId = {{warehouseId}}
                  AND ib.InventoryLotId = {{inventoryLotId}}
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
}
