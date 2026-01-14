using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Application.Interfaces;

public interface ITrainingRepository
{
    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TrainingSession>> GetUserTrainingHistoryAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
    Task AddAsync(TrainingSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrainingSession session, CancellationToken cancellationToken = default);
}
