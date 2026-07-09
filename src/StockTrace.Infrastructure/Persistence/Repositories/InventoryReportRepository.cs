using StockTrace.Application.Reports;
using StockTrace.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence.Repositories;

internal sealed class InventoryReportRepository(ApplicationDbContext dbContext) : IInventoryReportRepository
{
    public async Task<PagedResult<InventoryMovementReportItem>> GetMovementsAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken)
    {
        var movements = dbContext.InventoryMovements.AsNoTracking().AsQueryable();
        if (query.WarehouseId.HasValue)
            movements = movements.Where(x => x.WarehouseId == query.WarehouseId.Value);
        if (query.SupplierId.HasValue)
            movements = movements.Where(x => x.InventoryLot.SupplierId == query.SupplierId.Value);
        if (query.CategoryId.HasValue)
            movements = movements.Where(x => x.Product.CategoryId == query.CategoryId.Value);
        if (query.ProductId.HasValue)
            movements = movements.Where(x => x.ProductId == query.ProductId.Value);
        if (query.From.HasValue)
            movements = movements.Where(x => x.OccurredAt >= query.From.Value);
        if (query.To.HasValue)
            movements = movements.Where(x => x.OccurredAt <= query.To.Value);

        var totalCount = await movements.CountAsync(cancellationToken);
        var rows = await movements
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ReportRow(
                x.Id, x.OccurredAt, x.WarehouseId, x.Warehouse.Name,
                x.ProductId, x.Product.Sku, x.Product.Name, x.Product.CategoryId, x.Product.Category.Name,
                x.InventoryLot.SupplierId, x.InventoryLot.Supplier.Name,
                x.InventoryLotId, x.InventoryLot.LotNumber, x.MovementType,
                x.Quantity, x.UnitCost, x.ReferenceType, x.ReferenceId))
            .ToArrayAsync(cancellationToken);

        var items = rows.Select(x => new InventoryMovementReportItem(
            x.MovementId, x.OccurredAt, x.WarehouseId, x.WarehouseName,
            x.ProductId, x.ProductSku, x.ProductName, x.CategoryId, x.CategoryName,
            x.SupplierId, x.SupplierName, x.InventoryLotId, x.LotNumber, x.MovementType.ToString(),
            x.Quantity, x.UnitCost, x.Quantity * x.UnitCost, x.ReferenceType, x.ReferenceId)).ToArray();

        return new PagedResult<InventoryMovementReportItem>(items, query.PageNumber, query.PageSize, totalCount);
    }

    private sealed record ReportRow(
        Guid MovementId,
        DateTimeOffset OccurredAt,
        Guid WarehouseId,
        string WarehouseName,
        Guid ProductId,
        string ProductSku,
        string ProductName,
        Guid CategoryId,
        string CategoryName,
        Guid SupplierId,
        string SupplierName,
        Guid InventoryLotId,
        string LotNumber,
        InventoryMovementType MovementType,
        decimal Quantity,
        decimal UnitCost,
        string ReferenceType,
        Guid ReferenceId);
}
