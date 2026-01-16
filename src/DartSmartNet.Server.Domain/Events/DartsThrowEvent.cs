namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when a dart is thrown (before all 3 darts are pulled)
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
        GameMode,
        PointsLeft,
        DartNumber,
        Segment,
        Multiplier,
        Points
    );

    public record GameEventData(
        string Mode,
        int PointsLeft,
        int DartNumber,
        int Segment,
        int Multiplier,
        int Points
    );
}
