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
        // CRITICAL: Clear tracker to prevent stale entity issues
        // This is safe because SemaphoreSlim in GameService ensures sequential processing per game
        _context.ChangeTracker.Clear();

        return await _context.GameSessions
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Include(g => g.Throws)
            .Include(g => g.Winner)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets a game by ID with pessimistic row lock (SELECT FOR UPDATE)
    /// This prevents concurrent modifications to the same game
    /// </summary>
    public async Task<GameSession?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Clear ALL tracked entities to prevent state conflicts
        // This is critical for pessimistic locking to work correctly
        _context.ChangeTracker.Clear();

        // Use raw SQL with FOR UPDATE to acquire row lock
        // This blocks other transactions from modifying this row until commit
        var game = await _context.GameSessions
            .FromSqlRaw("SELECT * FROM game_sessions WHERE id = {0} FOR UPDATE", id)
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .Include(g => g.Throws)
            .Include(g => g.Winner)
            .FirstOrDefaultAsync(cancellationToken);

        return game;
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

    /// <summary>
    /// Adds a new DartThrow to a game and explicitly marks it for insertion
    /// This prevents EF Core from treating it as an existing entity
    /// </summary>
    public void AddThrowToGame(GameSession game, DartThrow dartThrow)
    {
        // CRITICAL: Explicitly set entity state to Added FIRST, before any collection manipulation
        // This ensures EF Core will generate INSERT, not UPDATE
        // When a GameSession is loaded with Include(g => g.Throws), EF Core tracks the Throws collection.
        // Adding to that collection can cause EF Core to treat entities incorrectly if they have pre-set GUIDs.
        _context.Entry(dartThrow).State = EntityState.Added;
        
        // Now add to domain collection (for domain logic / stats updates)
        game.AddThrow(dartThrow);
    }

    public async Task UpdateAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        try
        {
            // With ChangeTracker.Clear() + explicit AddThrowToGame(),
            // EF Core now correctly tracks entity states
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            throw;
        }
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
