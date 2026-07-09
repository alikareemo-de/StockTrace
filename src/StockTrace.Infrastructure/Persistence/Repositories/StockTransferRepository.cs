using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Transfers;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Transfers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class StockTransferRepository(ApplicationDbContext dbContext) : IStockTransferRepository
{
    public Task<bool> TransferNumberExistsAsync(string transferNumber, CancellationToken cancellationToken) =>
        dbContext.StockTransfers.AnyAsync(x => x.TransferNumber == transferNumber, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetExistingWarehouseIdsAsync(
        IEnumerable<Guid> warehouseIds,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.Warehouses
            .Where(x => warehouseIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.Products
            .Where(x => productIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task AddAsync(
        StockTransfer transfer,
        IReadOnlyCollection<InventoryBalance> newDestinationBalances,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken cancellationToken)
    {
        dbContext.StockTransfers.Add(transfer);
        dbContext.InventoryBalances.AddRange(newDestinationBalances);
        dbContext.InventoryMovements.AddRange(movements);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Inventory changed while the transfer was being processed. Retry the operation.");
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is SqlException { Number: 2601 or 2627 or 547 or 1205 })
        {
            throw new ConflictException("The transfer conflicts with current inventory or an existing transfer number.");
        }
    }

    public async Task<StockTransferResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.StockTransfers.AsNoTracking()
            .Include(x => x.Lines)
                .ThenInclude(x => x.Allocations)
                    .ThenInclude(x => x.InventoryLot)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transfer is null) return null;

        return new StockTransferResult(
            transfer.Id, transfer.TransferNumber, transfer.SourceWarehouseId, transfer.DestinationWarehouseId,
            transfer.Status.ToString(), transfer.CompletedAt,
            transfer.Lines.Select(line => new StockTransferLineResult(
                line.Id, line.ProductId, line.RequestedQuantity,
                line.Allocations.Select(x => new TransferAllocationResult(
                    x.InventoryLotId, x.InventoryLot.LotNumber, x.InventoryLot.SupplierId, x.Quantity,
                    x.InventoryLot.UnitCost)).ToArray())).ToArray());
    }
}
