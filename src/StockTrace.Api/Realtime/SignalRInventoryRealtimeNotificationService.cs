using StockTrace.Application.Inventory;
using StockTrace.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Api.Realtime;

internal sealed class SignalRInventoryRealtimeNotificationService(
    ApplicationDbContext dbContext,
    IHubContext<LowStockHub, ILowStockClient> hubContext) : IInventoryRealtimeNotificationService
{
    public async Task PublishAsync(
        IReadOnlyCollection<LowStockCheck> checks,
        CancellationToken cancellationToken)
    {
        if (checks.Count == 0)
        {
            return;
        }

        var distinctChecks = checks
            .GroupBy(x => new { x.WarehouseId, x.ProductId })
            .Select(x => x.Last())
            .ToArray();

        var warehouseIds = distinctChecks.Select(x => x.WarehouseId).Distinct().ToArray();
        var productIds = distinctChecks.Select(x => x.ProductId).Distinct().ToArray();

        var warehouses = await dbContext.Warehouses
            .AsNoTracking()
            .Where(x => warehouseIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.Sku, x.Name, x.DefaultLowStockThreshold })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var thresholds = await dbContext.WarehouseProductSettings
            .AsNoTracking()
            .Where(x => warehouseIds.Contains(x.WarehouseId) && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => (x.WarehouseId, x.ProductId), x => x.LowStockThreshold, cancellationToken);

        var stockChangedAlerts = new List<StockChangedAlert>();
        var lowStockAlerts = new List<LowStockAlert>();

        foreach (var check in distinctChecks)
        {
            if (!warehouses.TryGetValue(check.WarehouseId, out var warehouse) ||
                !products.TryGetValue(check.ProductId, out var product))
            {
                continue;
            }

            stockChangedAlerts.Add(new StockChangedAlert(
                check.WarehouseId,
                warehouse.Name,
                check.ProductId,
                product.Sku,
                product.Name,
                check.QuantityBefore,
                check.QuantityAfter,
                check.OccurredAt,
                check.TriggeredBy));

            var threshold = thresholds.TryGetValue((check.WarehouseId, check.ProductId), out var configuredThreshold)
                ? configuredThreshold
                : product.DefaultLowStockThreshold;

            if (threshold <= 0)
            {
                continue;
            }

            if (check.QuantityBefore > threshold && check.QuantityAfter <= threshold)
            {
                lowStockAlerts.Add(new LowStockAlert(
                    check.WarehouseId,
                    warehouse.Name,
                    check.ProductId,
                    product.Sku,
                    product.Name,
                    threshold,
                    check.QuantityAfter,
                    check.OccurredAt,
                    check.TriggeredBy));
            }
        }

        foreach (var alert in stockChangedAlerts)
        {
            await hubContext.Clients.All.StockChanged(alert);
        }

        foreach (var alert in lowStockAlerts)
        {
            await hubContext.Clients.All.LowStockReached(alert);
        }
    }
}
