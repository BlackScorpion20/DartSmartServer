using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Services;

public interface IMatchmakingService
{
    Task<Guid?> JoinQueueAsync(Guid userId, GameType gameType, int? startingScore, CancellationToken cancellationToken = default);
    Task LeaveQueueAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetQueueCountAsync(GameType gameType, CancellationToken cancellationToken = default);
}
