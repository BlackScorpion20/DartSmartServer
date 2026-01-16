using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class GameProfileRepository : IGameProfileRepository
{
    private readonly ApplicationDbContext _context;

    public GameProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GameProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GameProfiles
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.ProfileId == id, cancellationToken);
    }

    public async Task<IEnumerable<GameProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.GameProfiles
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameProfile>> GetPublicProfilesAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.GameProfiles
            .Include(p => p.Owner)
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GameProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.GameProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GameProfile profile, CancellationToken cancellationToken = default)
    {
        _context.GameProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(GameProfile profile, CancellationToken cancellationToken = default)
    {
        _context.GameProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
