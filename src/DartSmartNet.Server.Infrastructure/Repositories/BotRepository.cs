using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class BotRepository : IBotRepository
{
    private readonly ApplicationDbContext _context;

    public BotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Bot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bots
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Bot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bots.ToListAsync(cancellationToken);
    }

    public async Task<Bot?> GetByDifficultyAsync(BotDifficulty difficulty, CancellationToken cancellationToken = default)
    {
        return await _context.Bots
            .FirstOrDefaultAsync(b => b.Difficulty == difficulty, cancellationToken);
    }

    public async Task AddAsync(Bot bot, CancellationToken cancellationToken = default)
    {
        await _context.Bots.AddAsync(bot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
