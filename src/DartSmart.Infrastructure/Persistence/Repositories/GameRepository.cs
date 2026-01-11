using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DartSmart.Infrastructure.Persistence.Repositories;

public class GameRepository : IGameRepository
{
    private readonly DartSmartDbContext _context;

    public GameRepository(DartSmartDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(GameId id, CancellationToken cancellationToken = default)
    {
        return await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game?> GetByIdWithPlayersAsync(GameId id, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game?> GetByIdWithThrowsAsync(GameId id, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Throws)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetActiveGamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Players)
            .Where(g => g.Status == GameStatus.Lobby || g.Status == GameStatus.InProgress)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Players)
            .Where(g => g.Players.Any(p => p.PlayerId == playerId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Players)
            .Where(g => g.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _context.Games.AddAsync(game, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(GameId id, CancellationToken cancellationToken = default)
    {
        return await _context.Games.AnyAsync(g => g.Id == id, cancellationToken);
    }
}
