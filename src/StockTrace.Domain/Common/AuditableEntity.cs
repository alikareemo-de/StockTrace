namespace StockTrace.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity() { }
    protected AuditableEntity(Guid id) : base(id) { }

    public DateTimeOffset CreatedAt { get; protected set; }
    public string CreatedBy { get; protected set; } = string.Empty;
    public DateTimeOffset? ModifiedAt { get; protected set; }
    public string? ModifiedBy { get; protected set; }
}

public abstract class SoftDeletableEntity : AuditableEntity
{
    protected SoftDeletableEntity() { }
    protected SoftDeletableEntity(Guid id) : base(id) { }

    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }
}
