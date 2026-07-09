using StockTrace.Domain.Catalog;
using StockTrace.Domain.Common;

namespace StockTrace.Domain.Warehousing;

public sealed class WarehouseProductSetting : AuditableEntity
{
    private WarehouseProductSetting() { }

    public WarehouseProductSetting(Guid warehouseId, Guid productId, decimal lowStockThreshold) : base(Guid.NewGuid())
    {
        WarehouseId = warehouseId;
        ProductId = productId;
        LowStockThreshold = Guard.NotNegative(lowStockThreshold, nameof(lowStockThreshold));
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public decimal LowStockThreshold { get; private set; }

    public void UpdateLowStockThreshold(decimal lowStockThreshold)
    {
        LowStockThreshold = Guard.NotNegative(lowStockThreshold, nameof(lowStockThreshold));
    }
}
