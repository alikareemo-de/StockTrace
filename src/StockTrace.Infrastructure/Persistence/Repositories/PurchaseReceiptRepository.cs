using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Purchases;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Purchasing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class PurchaseReceiptRepository(ApplicationDbContext dbContext) : IPurchaseReceiptRepository
{
    public Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken) =>
        dbContext.PurchaseReceipts.AnyAsync(x => x.ReceiptNumber == receiptNumber, cancellationToken);

    public Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken cancellationToken) =>
        dbContext.Suppliers.AnyAsync(x => x.Id == supplierId && !x.IsDeleted, cancellationToken);

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

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetQuantitiesOnHandAsync(
        Guid warehouseId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.ToArray();
        return await dbContext.InventoryBalances
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && ids.Contains(x.InventoryLot.ProductId))
            .GroupBy(x => x.InventoryLot.ProductId)
            .Select(x => new { ProductId = x.Key, QuantityOnHand = x.Sum(balance => balance.QuantityOnHand) })
            .ToDictionaryAsync(x => x.ProductId, x => x.QuantityOnHand, cancellationToken);
    }

    public async Task AddAsync(
        PurchaseReceipt receipt,
        IReadOnlyCollection<InventoryLot> lots,
        IReadOnlyCollection<InventoryBalance> balances,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken cancellationToken)
    {
        dbContext.PurchaseReceipts.Add(receipt);
        dbContext.InventoryLots.AddRange(lots);
        dbContext.InventoryBalances.AddRange(balances);
        dbContext.InventoryMovements.AddRange(movements);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException($"Purchase receipt '{receipt.ReceiptNumber}' already exists.");
        }
    }

    public async Task<PurchaseReceiptResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var receipt = await dbContext.PurchaseReceipts
            .AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (receipt is null) return null;

        var lotsByLineId = await dbContext.InventoryLots
            .AsNoTracking()
            .Where(x => x.PurchaseReceiptLine.PurchaseReceiptId == id)
            .ToDictionaryAsync(x => x.PurchaseReceiptLineId, cancellationToken);

        var lines = receipt.Lines.Select(line =>
        {
            var lot = lotsByLineId[line.Id];
            return new PurchaseReceiptLineResult(
                line.Id, line.ProductId, line.Quantity, line.UnitCost, lot.Id, lot.LotNumber);
        }).ToArray();

        return new PurchaseReceiptResult(receipt.Id, receipt.ReceiptNumber, receipt.SupplierId,
            receipt.WarehouseId, receipt.ReceivedAt, receipt.Status.ToString(), lines);
    }
}
