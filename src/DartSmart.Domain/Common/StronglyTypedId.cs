namespace DartSmart.Domain.Common;

/// <summary>
/// Base class for strongly-typed IDs
/// </summary>
public abstract record StronglyTypedId<TValue>(TValue Value) where TValue : notnull
{
    public override string ToString() => Value.ToString() ?? string.Empty;
}

/// <summary>
/// Strongly-typed ID for Player aggregate
/// </summary>
public sealed record PlayerId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public static PlayerId From(Guid value) => new(value);
}

/// <summary>
/// Strongly-typed ID for Game aggregate
/// </summary>
public sealed record GameId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static GameId New() => new(Guid.NewGuid());
    public static GameId From(Guid value) => new(value);
}

/// <summary>
/// Strongly-typed ID for DartThrow entity
/// </summary>
public sealed record DartThrowId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static DartThrowId New() => new(Guid.NewGuid());
    public static DartThrowId From(Guid value) => new(value);
}
