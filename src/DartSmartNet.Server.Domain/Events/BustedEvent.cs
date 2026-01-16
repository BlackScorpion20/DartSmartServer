namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when a player busts (goes over in X01 games)
/// </summary>
public sealed record BustedEvent(
    Guid GameId,
    DateTime Timestamp,
    string GameMode,
    string PlayerUsername,
    int PointsBefore,
    int PointsThrown
) : GameEvent("busted", GameId, Timestamp, GameMode)
{
    public string Player { get; init; } = PlayerUsername;
    public GameEventData Game { get; init; } = new(GameMode, PointsBefore, PointsThrown);

    public record GameEventData(string Mode, int PointsBefore, int PointsThrown);
}
