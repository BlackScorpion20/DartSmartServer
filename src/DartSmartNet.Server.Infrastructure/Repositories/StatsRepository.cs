using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class StatsRepository : IStatsRepository
{
    private readonly ApplicationDbContext _context;

    public StatsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerStats?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PlayerStats
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<PlayerStats>> GetLeaderboardAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.PlayerStats
            .Include(s => s.User)
            .OrderByDescending(s => s.AveragePPD)
            .ThenByDescending(s => s.GamesPlayed)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default)
    {
        await _context.PlayerStats.AddAsync(stats, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlayerStats stats, CancellationToken cancellationToken = default)
    {
        _context.PlayerStats.Update(stats);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
