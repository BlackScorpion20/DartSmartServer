using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class TrainingRepository : ITrainingRepository
{
    private readonly ApplicationDbContext _context;

    public TrainingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .Include(ts => ts.Throws)
            .FirstOrDefaultAsync(ts => ts.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TrainingSession>> GetUserTrainingHistoryAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .Where(ts => ts.UserId == userId)
            .Include(ts => ts.Throws)
            .OrderByDescending(ts => ts.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        await _context.TrainingSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        _context.TrainingSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
