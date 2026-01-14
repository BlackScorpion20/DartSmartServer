using DartSmartNet.Server.Domain.Enums;
using System.Collections.Concurrent;

namespace DartSmartNet.Server.Application.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly IGameService _gameService;

    // In-memory queue for matchmaking
    // Key: GameType-StartingScore, Value: Queue of user IDs
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<MatchmakingEntry>> _queues = new();

    // Track which queue each user is in
    private static readonly ConcurrentDictionary<Guid, string> _userQueues = new();

    public MatchmakingService(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task<Guid?> JoinQueueAsync(
        Guid userId,
        GameType gameType,
        int? startingScore,
        CancellationToken cancellationToken = default)
    {
        // Create queue key
        var queueKey = GetQueueKey(gameType, startingScore);

        // Check if user is already in a queue
        if (_userQueues.ContainsKey(userId))
        {
            await LeaveQueueAsync(userId, cancellationToken);
        }

        // Get or create queue
        var queue = _queues.GetOrAdd(queueKey, _ => new ConcurrentQueue<MatchmakingEntry>());

        // Try to find a match from existing queue
        if (queue.TryDequeue(out var opponent))
        {
            // Found a match! Create game
            var playerIds = new[] { userId, opponent.UserId };

            var gameState = await _gameService.CreateGameAsync(
                gameType,
                startingScore,
                playerIds,
                isOnline: true,
                cancellationToken);

            // Remove opponent from user queues tracking
            _userQueues.TryRemove(opponent.UserId, out _);

            return gameState.GameId;
        }
        else
        {
            // No match found, add to queue
            var entry = new MatchmakingEntry(userId, gameType, startingScore);
            queue.Enqueue(entry);
            _userQueues[userId] = queueKey;

            return null; // No match yet
        }
    }

    public Task LeaveQueueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_userQueues.TryRemove(userId, out var queueKey))
        {
            // User was in a queue, try to remove them
            // Note: ConcurrentQueue doesn't support efficient removal
            // In production, you'd use a better data structure
            if (_queues.TryGetValue(queueKey, out var queue))
            {
                // Rebuild queue without the user
                var tempList = new List<MatchmakingEntry>();
                while (queue.TryDequeue(out var entry))
                {
                    if (entry.UserId != userId)
                    {
                        tempList.Add(entry);
                    }
                }

                // Re-enqueue remaining entries
                foreach (var entry in tempList)
                {
                    queue.Enqueue(entry);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetQueueCountAsync(GameType gameType, CancellationToken cancellationToken = default)
    {
        var totalCount = 0;

        // Count all queues that match the game type
        foreach (var kvp in _queues)
        {
            if (kvp.Key.StartsWith($"{gameType}_"))
            {
                totalCount += kvp.Value.Count;
            }
        }

        return Task.FromResult(totalCount);
    }

    private static string GetQueueKey(GameType gameType, int? startingScore)
    {
        return startingScore.HasValue
            ? $"{gameType}_{startingScore}"
            : $"{gameType}_default";
    }

    private record MatchmakingEntry(Guid UserId, GameType GameType, int? StartingScore);
}
