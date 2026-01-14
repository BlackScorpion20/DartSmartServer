using DartSmartNet.Server.Application.DTOs.Stats;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IStatsRepository _statsRepository;
    private readonly IUserRepository _userRepository;

    public StatisticsService(
        IStatsRepository statsRepository,
        IUserRepository userRepository)
    {
        _statsRepository = statsRepository;
        _userRepository = userRepository;
    }

    public async Task<PlayerStatsDto?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var stats = await _statsRepository.GetByUserIdAsync(userId, cancellationToken);

        if (stats == null)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        return MapToDto(stats, user?.Username ?? "Unknown");
    }

    public async Task<IEnumerable<PlayerStatsDto>> GetLeaderboardAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var statsCollection = await _statsRepository.GetLeaderboardAsync(limit, cancellationToken);

        var leaderboard = new List<PlayerStatsDto>();

        foreach (var stats in statsCollection)
        {
            var user = await _userRepository.GetByIdAsync(stats.UserId, cancellationToken);
            leaderboard.Add(MapToDto(stats, user?.Username ?? "Unknown"));
        }

        return leaderboard;
    }

    public async Task UpdateStatsAfterGameAsync(
        Guid userId,
        bool won,
        int dartsThrown,
        int pointsScored,
        IEnumerable<int> roundScores,
        int checkout,
        CancellationToken cancellationToken = default)
    {
        var stats = await _statsRepository.GetByUserIdAsync(userId, cancellationToken);

        if (stats == null)
        {
            // Create new stats record for user
            stats = PlayerStats.CreateForUser(userId);
            stats.UpdateAfterGame(won, dartsThrown, pointsScored, roundScores, checkout);
            await _statsRepository.AddAsync(stats, cancellationToken);
        }
        else
        {
            // Update existing stats
            stats.UpdateAfterGame(won, dartsThrown, pointsScored, roundScores, checkout);
            await _statsRepository.UpdateAsync(stats, cancellationToken);
        }
    }

    private static PlayerStatsDto MapToDto(PlayerStats stats, string username)
    {
        return new PlayerStatsDto(
            stats.UserId,
            username,
            stats.GamesPlayed,
            stats.GamesWon,
            stats.GamesLost,
            stats.WinRate,
            stats.AveragePPD,
            stats.HighestCheckout,
            stats.Total180s,
            stats.Total171s,
            stats.Total140s
        );
    }
}
