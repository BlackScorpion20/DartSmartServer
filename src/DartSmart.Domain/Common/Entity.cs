namespace DartSmart.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}

/// <summary>
/// Base interface for aggregate roots
/// </summary>
public interface IAggregateRoot { }

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
