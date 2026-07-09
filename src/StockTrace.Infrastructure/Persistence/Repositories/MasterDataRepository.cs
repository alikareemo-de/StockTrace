using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.MasterData;
using StockTrace.Domain.Warehousing;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class MasterDataRepository(ApplicationDbContext dbContext) : IMasterDataRepository
{
    public async Task<IReadOnlyCollection<CategoryResult>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResult(x.Id, x.Name))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<SupplierResult>> GetSuppliersAsync(CancellationToken cancellationToken) =>
        await dbContext.Suppliers.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierResult(x.Id, x.Code, x.Name))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<WarehouseResult>> GetWarehousesAsync(CancellationToken cancellationToken) =>
        await dbContext.Warehouses.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseResult(x.Id, x.Code, x.Name, x.BranchName))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductResult>> GetProductsAsync(CancellationToken cancellationToken) =>
        await dbContext.Products.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Sku)
            .Select(x => new ProductResult(
                x.Id, x.Sku, x.Name, x.CategoryId, x.Category.Name,
                x.UnitOfMeasure, x.DefaultLowStockThreshold, x.IsActive))
            .ToArrayAsync(cancellationToken);

    public async Task<WarehouseProductThresholdResult> UpsertLowStockThresholdAsync(
        Guid warehouseId,
        Guid productId,
        decimal lowStockThreshold,
        CancellationToken cancellationToken)
    {
        var warehouseExists = await dbContext.Warehouses
            .AnyAsync(x => x.Id == warehouseId && !x.IsDeleted, cancellationToken);
        if (!warehouseExists) throw new NotFoundException($"Warehouse '{warehouseId}' was not found.");

        var productExists = await dbContext.Products
            .AnyAsync(x => x.Id == productId && x.IsActive && !x.IsDeleted, cancellationToken);
        if (!productExists) throw new NotFoundException($"Product '{productId}' was not found or inactive.");

        var setting = await dbContext.WarehouseProductSettings
            .SingleOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId, cancellationToken);

        if (setting is null)
        {
            setting = new WarehouseProductSetting(warehouseId, productId, lowStockThreshold);
            dbContext.WarehouseProductSettings.Add(setting);
        }
        else
        {
            setting.UpdateLowStockThreshold(lowStockThreshold);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new WarehouseProductThresholdResult(warehouseId, productId, lowStockThreshold);
    }
}
