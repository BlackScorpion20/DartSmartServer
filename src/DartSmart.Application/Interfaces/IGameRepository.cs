using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Application.Interfaces;

/// <summary>
/// Repository interface for Game aggregate
/// </summary>
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(GameId id, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdWithPlayersAsync(GameId id, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdWithThrowsAsync(GameId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Game>> GetActiveGamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Game>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Game>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(GameId id, CancellationToken cancellationToken = default);
}
