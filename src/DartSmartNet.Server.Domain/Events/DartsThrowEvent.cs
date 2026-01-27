namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when a dart is thrown
/// </summary>
public sealed record DartsThrowEvent(
    Guid GameId,
    DateTime Timestamp,
    string GameMode,
    string PlayerUsername,
    int PointsLeft,
    int DartNumber,
    int Segment,
    int Multiplier,
    int Points
) : GameEvent("darts-thrown", GameId, Timestamp, GameMode)
{
    public string Player { get; init; } = PlayerUsername;
    public GameEventData Game { get; init; } = new(
        Mode: GameMode,
        PointsLeft: PointsLeft,
        DartNumber: DartNumber,
        Segment: Segment,
        Multiplier: Multiplier,
        Points: Points
    );
}
