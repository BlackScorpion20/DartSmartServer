using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Interfaces;

public interface IBotRepository
{
    Task<Bot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bot?> GetByDifficultyAsync(BotDifficulty difficulty, CancellationToken cancellationToken = default);
    Task AddAsync(Bot bot, CancellationToken cancellationToken = default);
}
