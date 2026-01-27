using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface IGameService
{
    /// <summary>
    /// Creates a game with simple player IDs (backward compatible)
    /// </summary>
    Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        Guid[] playerIds,
        bool isOnline,
        GameOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a game with full player setup including type and display name
    /// </summary>
    Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        PlayerSetupDto[] players,
        bool isOnline,
        GameOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<GameStateDto> GetGameStateAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GameStateDto> StartGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GameStateDto> RegisterThrowAsync(Guid gameId, Guid userId, Score score, byte[]? rawData = null, CancellationToken cancellationToken = default);
    Task<GameStateDto> EndGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameStateDto>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
    Task<GameSession?> GetGameEntityAsync(Guid gameId, CancellationToken cancellationToken = default);
    GameStatsUpdatedDto CalculateIncrementalStats(GameSession game, CancellationToken cancellationToken = default);
}

