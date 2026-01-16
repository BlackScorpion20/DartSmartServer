using System;
using System.Collections.Generic;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.DTOs.Game;

public sealed record GameStateDto(
    Guid GameId,
    GameType GameType,
    int? StartingScore,
    GameStatus Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    Guid? WinnerId,
    bool IsOnline,
    bool IsBotGame,
    List<PlayerGameDto> Players,
    int CurrentPlayerIndex,
    GameOptions? Options = null
);

public sealed record PlayerGameDto(
    Guid UserId,
    string Username,
    int PlayerOrder,
    int? FinalScore,
    int DartsThrown,
    int PointsScored,
    decimal PPD,
    bool IsWinner,
    Dictionary<int, int>? CricketMarks = null
);
