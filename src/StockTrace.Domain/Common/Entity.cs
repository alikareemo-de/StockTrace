namespace StockTrace.Domain.Common;

public abstract class Entity
{
    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
    }

    public Guid Id { get; protected set; }
}
