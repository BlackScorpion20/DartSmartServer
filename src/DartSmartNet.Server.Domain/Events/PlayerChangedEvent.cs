namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when turn changes to next player
/// </summary>
public sealed record PlayerChangedEvent(
    Guid GameId,
    DateTime Timestamp,
    string GameMode,
    string PlayerUsername,
    int PointsLeft,
    int PlayerOrder
) : GameEvent("player-changed", GameId, Timestamp, GameMode)
{
    public string Player { get; init; } = PlayerUsername;
    public GameEventData Game { get; init; } = new(GameMode, PointsLeft, PlayerOrder);

    public record GameEventData(string Mode, int PointsLeft, int PlayerOrder);
}
