using StockTrace.Domain.Common;

namespace StockTrace.Domain.Warehousing;

public sealed class Warehouse : SoftDeletableEntity
{
    private Warehouse() { }

    public Warehouse(string code, string name, string branchName) : base(Guid.NewGuid())
    {
        Code = Guard.Required(code, nameof(code), 50);
        Name = Guard.Required(name, nameof(name), 200);
        BranchName = Guard.Required(branchName, nameof(branchName), 200);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string BranchName { get; private set; } = string.Empty;
}
