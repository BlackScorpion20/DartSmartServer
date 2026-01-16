using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface IMatchmakingService
{
    Task<Guid?> JoinQueueAsync(Guid userId, GameType gameType, int? startingScore, CancellationToken cancellationToken = default);
    Task<Guid?> JoinQueueWithSkillAsync(Guid userId, GameType gameType, int? startingScore, SkillRating skillRating, CancellationToken cancellationToken = default);
    Task LeaveQueueAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetQueueCountAsync(GameType gameType, CancellationToken cancellationToken = default);
    Task<MatchmakingStatusDto> GetQueueStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}
