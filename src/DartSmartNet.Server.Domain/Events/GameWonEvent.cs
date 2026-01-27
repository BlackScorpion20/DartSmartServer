namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Event fired when a player wins the game
/// </summary>
public sealed record GameWonEvent(
    Guid GameId,
    DateTime Timestamp,
    string GameMode,
    string PlayerUsername,
    int DartsThrown,
    int TotalPoints,
    double AveragePPD
) : GameEvent("game-won", GameId, Timestamp, GameMode)
{
    public string Player { get; init; } = PlayerUsername;
    public GameEventData Game { get; init; } = new(
        Mode: GameMode,
        DartsThrown: DartsThrown,
        TotalPoints: TotalPoints,
        AveragePPD: AveragePPD
    );
}
