using StockTrace.Domain.Common;

namespace StockTrace.Domain.Catalog;

public sealed class Product : SoftDeletableEntity
{
    private Product() { }

    public Product(string sku, string name, Guid categoryId, string unitOfMeasure, decimal defaultLowStockThreshold = 0)
        : base(Guid.NewGuid())
    {
        Sku = Guard.Required(sku, nameof(sku), 50);
        Name = Guard.Required(name, nameof(name), 200);
        CategoryId = categoryId;
        UnitOfMeasure = Guard.Required(unitOfMeasure, nameof(unitOfMeasure), 30);
        DefaultLowStockThreshold = Guard.NotNegative(defaultLowStockThreshold, nameof(defaultLowStockThreshold));
    }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal DefaultLowStockThreshold { get; private set; }
    public bool IsActive { get; private set; } = true;
}
