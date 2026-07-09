using StockTrace.Domain.Common;

namespace StockTrace.Domain.Partners;

public sealed class Supplier : SoftDeletableEntity
{
    private Supplier() { }

    public Supplier(string code, string name) : base(Guid.NewGuid())
    {
        Code = Guard.Required(code, nameof(code), 50);
        Name = Guard.Required(name, nameof(name), 200);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}
