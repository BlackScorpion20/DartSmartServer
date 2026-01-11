using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartSmart.Infrastructure.Persistence.Repositories;

public class DartThrowRepository : IDartThrowRepository
{
    private readonly DartSmartDbContext _context;

    public DartThrowRepository(DartSmartDbContext context)
    {
        _context = context;
    }

    public async Task<DartThrow?> GetByIdAsync(DartThrowId id, CancellationToken cancellationToken = default)
    {
        return await _context.DartThrows.FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DartThrow>> GetByGameIdAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _context.DartThrows
            .Where(dt => dt.GameId == gameId)
            .OrderBy(dt => dt.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DartThrow>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.DartThrows
            .Where(dt => dt.PlayerId == playerId)
            .OrderByDescending(dt => dt.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DartThrow>> GetByPlayerIdInPeriodAsync(
        PlayerId playerId, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        return await _context.DartThrows
            .Where(dt => dt.PlayerId == playerId && dt.Timestamp >= from && dt.Timestamp <= to)
            .OrderBy(dt => dt.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DartThrow dartThrow, CancellationToken cancellationToken = default)
    {
        await _context.DartThrows.AddAsync(dartThrow, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DartThrow> throws, CancellationToken cancellationToken = default)
    {
        await _context.DartThrows.AddRangeAsync(throws, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTotalThrowsAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.DartThrows.CountAsync(dt => dt.PlayerId == playerId, cancellationToken);
    }

    public async Task<int> Get180CountAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        // Count rounds where player scored 180 (three T20s)
        return await _context.DartThrows
            .Where(dt => dt.PlayerId == playerId && !dt.IsBust)
            .GroupBy(dt => new { dt.GameId, dt.Round })
            .Where(g => g.Sum(dt => dt.Points) == 180)
            .CountAsync(cancellationToken);
    }

    public async Task<decimal> GetAveragePerDartAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        var throws = await _context.DartThrows
            .Where(dt => dt.PlayerId == playerId && !dt.IsBust)
            .ToListAsync(cancellationToken);

        if (throws.Count == 0) return 0;

        return (decimal)throws.Sum(dt => dt.Points) / throws.Count;
    }
}
