namespace Dispatch.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
}
