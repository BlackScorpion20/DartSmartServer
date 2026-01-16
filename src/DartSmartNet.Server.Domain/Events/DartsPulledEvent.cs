namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when darts are pulled (all 3 darts thrown and turn complete)
/// </summary>
public sealed record DartsPulledEvent(
    Guid GameId,
    DateTime Timestamp,
    string GameMode,
    string PlayerUsername,
    int PointsLeft,
    int DartsThrown,
    int TotalPoints
) : GameEvent("darts-pulled", GameId, Timestamp, GameMode)
{
    public string Player { get; init; } = PlayerUsername;
    public GameEventData Game { get; init; } = new(
        GameMode,
        PointsLeft,
        DartsThrown,
        TotalPoints
    );

    public record GameEventData(
        string Mode,
        int PointsLeft,
        int DartsThrown,
        int TotalPoints
    );
}
