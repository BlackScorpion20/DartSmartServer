using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DartSmartNet.Server.Application.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchmakingService> _logger;

    // In-memory queue for matchmaking with skill rating
    private static readonly ConcurrentDictionary<string, ConcurrentBag<MatchmakingEntry>> _queues = new();

    // Track which queue each user is in
    private static readonly ConcurrentDictionary<Guid, string> _userQueues = new();

    // Maximum rating difference for matching (will widen over time)
    private const int BaseRatingRange = 150;
    private const int MaxRatingRange = 500;

    public MatchmakingService(IServiceScopeFactory scopeFactory, ILogger<MatchmakingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Guid?> JoinQueueAsync(
        Guid userId,
        GameType gameType,
        int? startingScore,
        CancellationToken cancellationToken = default)
    {
        // Get user's skill rating
        SkillRating skillRating;
        using (var scope = _scopeFactory.CreateScope())
        {
            var statsRepo = scope.ServiceProvider.GetRequiredService<IStatsRepository>();
            var stats = await statsRepo.GetByUserIdAsync(userId, cancellationToken);
            skillRating = stats != null 
                ? SkillRating.FromThreeDartAverage(stats.ThreeDartAverage, stats.GamesPlayed)
                : SkillRating.Default;
        }

        return await JoinQueueWithSkillAsync(userId, gameType, startingScore, skillRating, cancellationToken);
    }

    public async Task<Guid?> JoinQueueWithSkillAsync(
        Guid userId,
        GameType gameType,
        int? startingScore,
        SkillRating skillRating,
        CancellationToken cancellationToken = default)
    {
        var queueKey = GetQueueKey(gameType, startingScore);

        // Leave any existing queue
        if (_userQueues.ContainsKey(userId))
        {
            await LeaveQueueAsync(userId, cancellationToken);
        }

        var queue = _queues.GetOrAdd(queueKey, _ => new ConcurrentBag<MatchmakingEntry>());
        var entry = new MatchmakingEntry(userId, gameType, startingScore, skillRating, DateTime.UtcNow);

        // Try to find a skill-matched opponent
        var opponent = FindBestMatch(queue, entry);

        if (opponent != null)
        {
            // Found a match! Create game
            _logger.LogInformation(
                "Match found! Player {Player1} ({Rating1}) vs Player {Player2} ({Rating2})",
                userId, skillRating.Rating, opponent.UserId, opponent.SkillRating.Rating);

            using var scope = _scopeFactory.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            var playerIds = new[] { userId, opponent.UserId };

            var gameState = await gameService.CreateGameAsync(
                gameType,
                startingScore,
                playerIds,
                isOnline: true,
                options: null,
                cancellationToken: cancellationToken);

            _userQueues.TryRemove(opponent.UserId, out _);

            return gameState.GameId;
        }
        else
        {
            // No match found, add to queue
            queue.Add(entry);
            _userQueues[userId] = queueKey;

            _logger.LogInformation(
                "Player {UserId} ({Rating}) joined queue {QueueKey}. Queue size: {QueueSize}",
                userId, skillRating.Rating, queueKey, queue.Count);

            return null;
        }
    }

    private MatchmakingEntry? FindBestMatch(ConcurrentBag<MatchmakingEntry> queue, MatchmakingEntry seeker)
    {
        var candidates = queue.ToList();
        MatchmakingEntry? bestMatch = null;
        int bestScore = int.MaxValue;

        foreach (var candidate in candidates)
        {
            if (candidate.UserId == seeker.UserId)
                continue;

            // Calculate wait time bonus (wider range after waiting)
            var waitSeconds = (DateTime.UtcNow - candidate.JoinedAt).TotalSeconds;
            var adjustedRange = Math.Min(MaxRatingRange, BaseRatingRange + (int)(waitSeconds * 5));

            var ratingDiff = Math.Abs(seeker.SkillRating.Rating - candidate.SkillRating.Rating);

            if (ratingDiff <= adjustedRange)
            {
                // Lower score = better match
                var matchScore = ratingDiff;
                if (matchScore < bestScore)
                {
                    bestScore = matchScore;
                    bestMatch = candidate;
                }
            }
        }

        if (bestMatch != null)
        {
            // Remove matched player from queue
            var newQueue = new ConcurrentBag<MatchmakingEntry>(
                candidates.Where(c => c.UserId != bestMatch.UserId));
            _queues[GetQueueKey(seeker.GameType, seeker.StartingScore)] = newQueue;
        }

        return bestMatch;
    }

    public Task LeaveQueueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_userQueues.TryRemove(userId, out var queueKey))
        {
            if (_queues.TryGetValue(queueKey, out var queue))
            {
                var newQueue = new ConcurrentBag<MatchmakingEntry>(
                    queue.Where(e => e.UserId != userId));
                _queues[queueKey] = newQueue;
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetQueueCountAsync(GameType gameType, CancellationToken cancellationToken = default)
    {
        var totalCount = 0;

        foreach (var kvp in _queues)
        {
            if (kvp.Key.StartsWith($"{gameType}_"))
            {
                totalCount += kvp.Value.Count;
            }
        }

        return Task.FromResult(totalCount);
    }

    public Task<MatchmakingStatusDto> GetQueueStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_userQueues.TryGetValue(userId, out var queueKey))
        {
            return Task.FromResult(new MatchmakingStatusDto(false, 0, 0, null, null));
        }

        if (!_queues.TryGetValue(queueKey, out var queue))
        {
            return Task.FromResult(new MatchmakingStatusDto(false, 0, 0, null, null));
        }

        var entry = queue.FirstOrDefault(e => e.UserId == userId);
        if (entry == null)
        {
            return Task.FromResult(new MatchmakingStatusDto(false, 0, 0, null, null));
        }

        var waitTime = (int)(DateTime.UtcNow - entry.JoinedAt).TotalSeconds;
        var adjustedRange = Math.Min(MaxRatingRange, BaseRatingRange + waitTime * 5);

        return Task.FromResult(new MatchmakingStatusDto(
            true,
            queue.Count,
            waitTime,
            entry.SkillRating.TierName,
            adjustedRange
        ));
    }

    private static string GetQueueKey(GameType gameType, int? startingScore)
    {
        return startingScore.HasValue
            ? $"{gameType}_{startingScore}"
            : $"{gameType}_default";
    }

    private record MatchmakingEntry(
        Guid UserId, 
        GameType GameType, 
        int? StartingScore, 
        SkillRating SkillRating,
        DateTime JoinedAt);
}

public record MatchmakingStatusDto(
    bool InQueue,
    int QueueSize,
    int WaitTimeSeconds,
    string? SkillTier,
    int? SearchRangeRating
);
