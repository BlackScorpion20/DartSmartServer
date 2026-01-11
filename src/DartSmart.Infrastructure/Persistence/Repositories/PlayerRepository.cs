using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartSmart.Infrastructure.Persistence.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly DartSmartDbContext _context;

    public PlayerRepository(DartSmartDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken = default)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Players.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Player>> GetTopByStatisticAsync(
        string statistic, 
        int count = 10, 
        CancellationToken cancellationToken = default)
    {
        var query = statistic switch
        {
            "TotalGames" => _context.Players.OrderByDescending(p => p.Statistics.TotalGames),
            "TotalDarts" => _context.Players.OrderByDescending(p => p.Statistics.TotalDarts),
            "Average3Dart" => _context.Players.OrderByDescending(p => p.Statistics.TotalDarts > 0 
                ? (decimal)p.Statistics.TotalPoints / p.Statistics.TotalDarts * 3 : 0),
            "Count180s" => _context.Players.OrderByDescending(p => p.Statistics.Count180s),
            "HighestCheckout" => _context.Players.OrderByDescending(p => p.Statistics.HighestCheckout),
            "WinRate" => _context.Players.OrderByDescending(p => p.Statistics.TotalGames > 0 
                ? (decimal)p.Statistics.Wins / p.Statistics.TotalGames * 100 : 0),
            _ => _context.Players.OrderByDescending(p => p.Statistics.TotalGames)
        };

        return await query.Take(count).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        await _context.Players.AddAsync(player, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(PlayerId id, CancellationToken cancellationToken = default)
    {
        return await _context.Players.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Players.AnyAsync(p => p.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Players.AnyAsync(p => p.Username == username, cancellationToken);
    }
}
