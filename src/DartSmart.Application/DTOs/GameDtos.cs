using DartSmart.Domain.ValueObjects;

namespace DartSmart.Application.DTOs;

public record GameDto(
    string Id,
    string GameType,
    int StartScore,
    string InMode,
    string OutMode,
    string Status,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    string? WinnerId,
    int CurrentPlayerIndex,
    int CurrentRound,
    List<GamePlayerDto> Players
);

public record GamePlayerDto(
    string PlayerId,
    string Username,
    int CurrentScore,
    int TurnOrder,
    int DartsThrown,
    int LegsWon
);

public record GameSummaryDto(
    string Id,
    string GameType,
    int StartScore,
    string Status,
    int PlayerCount,
    DateTime CreatedAt
);

public record CreateGameDto(
    GameType GameType,
    int StartScore,
    X01InMode InMode = X01InMode.StraightIn,
    X01OutMode OutMode = X01OutMode.DoubleOut
);
