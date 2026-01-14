using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface IGameService
{
    Task<GameStateDto> CreateGameAsync(GameType gameType, int? startingScore, Guid[] playerIds, bool isOnline, CancellationToken cancellationToken = default);
    Task<GameStateDto> GetGameStateAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GameStateDto> StartGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GameStateDto> RegisterThrowAsync(Guid gameId, Guid userId, Score score, byte[]? rawData = null, CancellationToken cancellationToken = default);
    Task<GameStateDto> EndGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameStateDto>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
}
