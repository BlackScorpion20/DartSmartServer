using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Stats;

namespace DartSmartNet.Server.Application.Services;

public interface IStatisticsService
{
    Task<PlayerStatsDto?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlayerStatsDto>> GetLeaderboardAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task UpdateStatsAfterGameAsync(
        Guid userId,
        bool won,
        int dartsThrown,
        int pointsScored,
        IEnumerable<int> roundScores,
        int checkout,
        CancellationToken cancellationToken = default);
}
