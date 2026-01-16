namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Base class for all game events that can be broadcast to external systems
/// </summary>
public abstract record GameEvent(
    string EventType,
    Guid GameId,
    DateTime Timestamp,
    string GameMode
)
{
    public string EventType { get; init; } = EventType;
    public Guid GameId { get; init; } = GameId;
    public DateTime Timestamp { get; init; } = Timestamp;
    public string GameMode { get; init; } = GameMode;
}
