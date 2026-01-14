namespace DartSmartNet.Server.Application.DTOs.Stats;

public sealed record PlayerStatsDto(
    Guid UserId,
    string Username,
    int GamesPlayed,
    int GamesWon,
    int GamesLost,
    decimal WinRate,
    decimal AveragePPD,
    int HighestCheckout,
    int Total180s,
    int Total171s,
    int Total140s
);
