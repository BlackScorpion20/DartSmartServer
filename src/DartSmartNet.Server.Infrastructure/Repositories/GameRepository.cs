using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;

    public GameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GameSessions
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Include(g => g.Throws)
            .Include(g => g.Winner)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<GameSession>> GetActiveGamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GameSessions
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Where(g => g.Status == GameStatus.InProgress || g.Status == GameStatus.WaitingForPlayers)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameSession>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _context.GameSessions
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Where(g => g.Players.Any(p => p.UserId == userId))
            .OrderByDescending(g => g.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        await _context.GameSessions.AddAsync(game, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        _context.GameSessions.Update(game);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameSession>> GetGamesByTypeAsync(GameType gameType, CancellationToken cancellationToken = default)
    {
        return await _context.GameSessions
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Where(g => g.GameType == gameType)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
