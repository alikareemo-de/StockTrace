using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Sales;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Sales;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class SaleRepository(ApplicationDbContext dbContext) : ISaleRepository
{
    public Task<bool> SaleNumberExistsAsync(string saleNumber, CancellationToken cancellationToken) =>
        dbContext.Sales.AnyAsync(x => x.SaleNumber == saleNumber, cancellationToken);

    public Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        dbContext.Warehouses.AnyAsync(x => x.Id == warehouseId && !x.IsDeleted, cancellationToken);

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
        Sale sale,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken cancellationToken)
    {
        dbContext.Sales.Add(sale);
        dbContext.InventoryMovements.AddRange(movements);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Inventory changed while the sale was being processed. Retry the operation.");
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        {
            throw new ConflictException("The sale conflicts with current inventory or an existing sale number.");
        }
    }

    public async Task<SaleResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sale = await dbContext.Sales.AsNoTracking()
            .Include(x => x.Lines)
                .ThenInclude(x => x.Allocations)
                    .ThenInclude(x => x.InventoryLot)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sale is null) return null;

        return new SaleResult(sale.Id, sale.SaleNumber, sale.WarehouseId, sale.SoldAt, sale.Status.ToString(),
            sale.Lines.Select(line => new SaleLineResult(
                line.Id, line.ProductId, line.Quantity, line.UnitPrice,
                line.Allocations.Sum(x => x.Quantity * x.UnitCost),
                line.Allocations.Select(x => new SaleAllocationResult(
                    x.InventoryLotId, x.InventoryLot.LotNumber, x.InventoryLot.SupplierId, x.Quantity, x.UnitCost)).ToArray())).ToArray());
    }
}
