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
        int cricketMarks = 0,
        CancellationToken cancellationToken = default);

    Task UpdateStatsAfterGameAsync(
        Guid userId,
        bool won,
        int dartsThrown,
        int pointsScored,
        IEnumerable<int> roundScores,
        int checkout,
        int doubleAttempts,
        int doubleHits,
        decimal sessionAverage,
        decimal first9Average,
        int legsPlayed,
        int legsWon,
        int cricketMarks = 0,
        CancellationToken cancellationToken = default);
}
