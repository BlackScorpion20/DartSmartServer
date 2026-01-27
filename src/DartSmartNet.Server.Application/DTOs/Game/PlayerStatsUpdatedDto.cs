using System;

namespace DartSmartNet.Server.Application.DTOs.Game;

/// <summary>
/// DTO for real-time player statistics updates during a game
/// </summary>
public sealed record PlayerStatsUpdatedDto(
    Guid PlayerId,
    string PlayerName,
    int CurrentScore,
    int DartsThrown,
    int PointsScored,
    decimal PPD,
    decimal CurrentAverage,
    int RoundsCompleted,
    int LastThrowPoints,
    DateTime Timestamp
);

/// <summary>
/// Container for all players' stats in a game
/// </summary>
public sealed record GameStatsUpdatedDto(
    Guid GameId,
    List<PlayerStatsUpdatedDto> PlayerStats,
    DateTime Timestamp
);
