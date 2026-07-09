using StockTrace.Domain.Common;

namespace StockTrace.Domain.Catalog;

public sealed class Category : SoftDeletableEntity
{
    private Category() { }

    public Category(string name) : base(Guid.NewGuid())
    {
        Name = Guard.Required(name, nameof(name), 100);
    }

    public string Name { get; private set; } = string.Empty;
}
