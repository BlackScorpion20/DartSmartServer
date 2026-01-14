using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Application.Interfaces;

public interface IStatsRepository
{
    Task<PlayerStats?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlayerStats>> GetLeaderboardAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlayerStats stats, CancellationToken cancellationToken = default);
}
