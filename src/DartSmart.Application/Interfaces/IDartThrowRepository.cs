using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Interfaces;

/// <summary>
/// Repository interface for DartThrow time-series data
/// </summary>
public interface IDartThrowRepository
{
    Task<DartThrow?> GetByIdAsync(DartThrowId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DartThrow>> GetByGameIdAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DartThrow>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DartThrow>> GetByPlayerIdInPeriodAsync(
        PlayerId playerId, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default);
    Task AddAsync(DartThrow dartThrow, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DartThrow> throws, CancellationToken cancellationToken = default);
    
    // Statistics queries
    Task<int> GetTotalThrowsAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<int> Get180CountAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<decimal> GetAveragePerDartAsync(PlayerId playerId, CancellationToken cancellationToken = default);
}
