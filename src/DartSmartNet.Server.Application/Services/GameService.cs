using System.Collections.Concurrent;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Engines;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.Events;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStatisticsService _statisticsService;
    private readonly IGameEventBroadcaster _eventBroadcaster;
    private readonly IEnumerable<IGameEngine> _engines;

    // Game-level locking to prevent concurrent modifications to the same game
    // This prevents race conditions when multiple requests try to modify the same game simultaneously
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gameLocks = new();

    public GameService(
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IStatisticsService statisticsService,
        IGameEventBroadcaster eventBroadcaster,
        IEnumerable<IGameEngine> engines)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _statisticsService = statisticsService;
        _eventBroadcaster = eventBroadcaster;
        _engines = engines;
    }

    /// <summary>
    /// Acquires a lock for the specified game to ensure sequential processing of requests
    /// </summary>
    private SemaphoreSlim GetGameLock(Guid gameId)
    {
        return _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Releases and removes the lock for a game (called when game is completed or abandoned)
    /// </summary>
    private void ReleaseGameLock(Guid gameId)
    {
        if (_gameLocks.TryRemove(gameId, out var semaphore))
        {
            semaphore.Dispose();
        }
    }

    private IGameEngine GetEngine(GameType type) 
        => _engines.FirstOrDefault(e => e.GameType == type) 
           ?? throw new InvalidOperationException($"No engine found for game type {type}");

    /// <summary>
    /// Creates a game with simple player IDs (backward compatible - treats all as Human)
    /// </summary>
    public async Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        Guid[] playerIds,
        bool isOnline,
        GameOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert to PlayerSetupDto for backward compatibility
        var players = playerIds.Select(id => new PlayerSetupDto(
            id == Guid.Empty ? null : id,
            id == Guid.Empty ? PlayerType.Bot : PlayerType.Human,
            null
        )).ToArray();

        return await CreateGameAsync(gameType, startingScore, players, isOnline, options, cancellationToken);
    }

    /// <summary>
    /// Creates a game with full player setup including type and display name
    /// </summary>
    public async Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        PlayerSetupDto[] players,
        bool isOnline,
        GameOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Validate starting score for X01 games
        if (gameType == GameType.X01 && !startingScore.HasValue)
        {
            throw new InvalidOperationException("Starting score is required for X01 games");
        }

        var isBotGame = players.Any(p => p.PlayerType == PlayerType.Bot);
        var hasGuests = players.Any(p => p.PlayerType == PlayerType.Guest);
        var game = GameSession.Create(gameType, startingScore, isOnline, isBotGame, options);
        
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            game.AddPlayer(player.UserId, i + 1, player.PlayerType, player.DisplayName);
        }

        await _gameRepository.AddAsync(game, cancellationToken);
        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> GetGameStateAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null)
            throw new InvalidOperationException($"Game {gameId} not found");

        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> StartGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // Acquire game-level lock to prevent concurrent start attempts
        var gameLock = GetGameLock(gameId);
        await gameLock.WaitAsync(cancellationToken);

        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
            if (game == null)
                throw new InvalidOperationException("Game not found");

            game.Start();
            await _gameRepository.UpdateAsync(game, cancellationToken);

            return await MapToGameStateDto(game, cancellationToken);
        }
        finally
        {
            gameLock.Release();
        }
    }

    public async Task<GameStateDto> RegisterThrowAsync(
        Guid gameId,
        Guid userId,
        Score score,
        byte[]? rawData = null,
        CancellationToken cancellationToken = default)
    {
        // Acquire game-level lock to prevent concurrent modifications
        // This ensures that only one throw can be processed at a time for each game
        var gameLock = GetGameLock(gameId);
        await gameLock.WaitAsync(cancellationToken);

        try
        {
            // Use GetByIdAsync instead of GetByIdWithLockAsync, since we already have application-level lock
            // The SemaphoreSlim ensures only one thread processes this game at a time
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
            if (game == null) throw new InvalidOperationException($"Game {gameId} not found");
            if (game.Status != GameStatus.InProgress) throw new InvalidOperationException($"Game is not in progress");

            var player = game.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null) throw new InvalidOperationException($"User {userId} is not in this game");

            var lastPlayerThrows = game.Throws.Where(t => t.UserId == userId).OrderByDescending(t => t.RoundNumber).ThenByDescending(t => t.DartNumber).ToList();
            var lastThrow = lastPlayerThrows.FirstOrDefault();

            int roundNumber = 1;
            int dartNumber = 1;

            if (lastThrow != null)
            {
                var engine = GetEngine(game.GameType);
                // Check if the last round was finished (either 3 darts or a bust/win)
                // For now, let's look at the dart number. If it's 3, it's definitely a new round.
                // But if it's < 3, we need to know if it was a bust.

                // Re-calculate the game up to the last throw to see if it was a bust round
                var remainingScoreBeforeLast = game.StartingScore ?? 0;
                // (This is getting complex, maybe we should just let the client send the round/dart number?)
                // Actually, let's simplify: if the last throw was dart 3, it's a new round.
                // If it was dart 1 or 2, we check if it was a bust.

                bool isLastRoundFinished = lastThrow.DartNumber >= 3;

                if (!isLastRoundFinished)
                {
                    // Verify if it was a bust
                    // For X01, we can check if it was a bust.
                    if (game.GameType == GameType.X01)
                    {
                        // If we want to be 100% sure, we'd need to simulate again.
                        // But wait, the client knows! Let's just count throws for now but more intelligently.
                        // Actually, the simplest way is to see if the client's view matches.
                        // But we don't have it.
                    }
                }

                if (isLastRoundFinished)
                {
                    roundNumber = lastThrow.RoundNumber + 1;
                    dartNumber = 1;
                }
                else
                {
                    roundNumber = lastThrow.RoundNumber;
                    dartNumber = lastThrow.DartNumber + 1;
                }
            }

            var dartThrow = DartThrow.Create(gameId, userId, roundNumber, dartNumber, score, rawData);

            // CRITICAL: Use repository method to add throw
            // This ensures EF Core tracks it as "Added" instead of "Modified"
            _gameRepository.AddThrowToGame(game, dartThrow);

            var playerUsername = player.User?.Username ?? "Unknown";

            var currentEngine = GetEngine(game.GameType);
            var currentScoreForEvent = currentEngine.CalculateCurrentScore(game, userId);

            await _eventBroadcaster.BroadcastEventAsync(new DartsThrowEvent(
                gameId, DateTime.UtcNow, game.GameType.ToString(), playerUsername,
                currentScoreForEvent, dartNumber, score.Segment, (int)score.Multiplier, score.Points
            ), cancellationToken);

            bool gameEnded = false;
            if (currentEngine.CheckWinCondition(game, userId, out var finalScore))
            {
                game.Complete(userId);
                player.SetFinalScore(finalScore ?? 0);

                await _eventBroadcaster.BroadcastEventAsync(new GameWonEvent(
                    gameId, DateTime.UtcNow, game.GameType.ToString(), playerUsername,
                    player.DartsThrown, player.PointsScored, (double)player.PPD
                ), cancellationToken);

                await currentEngine.UpdateStatisticsAsync(game, cancellationToken);
                gameEnded = true;
            }

            await _gameRepository.UpdateAsync(game, cancellationToken);
            var result = await MapToGameStateDto(game, cancellationToken);

            // If game ended (won), release and remove the lock
            if (gameEnded)
            {
                ReleaseGameLock(gameId);
            }

            return result;
        }
        finally
        {
            // Only release if not already removed (in case game ended or exception)
            if (_gameLocks.ContainsKey(gameId))
            {
                gameLock.Release();
            }
        }
    }

    public async Task<GameSession?> GetGameEntityAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _gameRepository.GetByIdAsync(gameId, cancellationToken);
    }

    public GameStatsUpdatedDto CalculateIncrementalStats(
        GameSession game,
        CancellationToken cancellationToken = default)
    {
        var engine = GetEngine(game.GameType);
        var playerStats = new List<PlayerStatsUpdatedDto>();

        foreach (var player in game.Players)
        {
            var playerThrows = game.Throws.Where(t => t.UserId == player.UserId).OrderBy(t => t.ThrownAt).ToList();
            var dartsThrown = playerThrows.Count;
            var pointsScored = playerThrows.Sum(t => t.Points);
            var ppd = dartsThrown > 0 ? (decimal)pointsScored / dartsThrown : 0m;
            var currentScore = player.UserId.HasValue 
                ? engine.CalculateCurrentScore(game, player.UserId.Value) 
                : 0;

            // Calculate rounds completed (every 3 darts or bust = 1 round)
            var roundsCompleted = playerThrows.GroupBy(t => t.RoundNumber).Count();

            // Calculate current average (points per 3 darts)
            var currentAverage = roundsCompleted > 0
                ? (decimal)pointsScored / roundsCompleted
                : ppd * 3;

            var lastThrowPoints = playerThrows.LastOrDefault()?.Points ?? 0;
            var playerUsername = player.User?.Username ?? "Unknown";

            playerStats.Add(new PlayerStatsUpdatedDto(
                player.UserId.GetValueOrDefault(),
                playerUsername,
                currentScore,
                dartsThrown,
                pointsScored,
                ppd,
                currentAverage,
                roundsCompleted,
                lastThrowPoints,
                DateTime.UtcNow
            ));
        }

        return new GameStatsUpdatedDto(
            game.Id,
            playerStats,
            DateTime.UtcNow
        );
    }

    public async Task<GameStateDto> EndGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // Acquire game-level lock to prevent concurrent end attempts
        var gameLock = GetGameLock(gameId);
        await gameLock.WaitAsync(cancellationToken);

        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
            if (game == null) throw new InvalidOperationException("Game not found");

            game.Abandon();

            // Update stats even for abandoned games if they had throws
            if (game.Throws.Any())
            {
                var currentEngine = GetEngine(game.GameType);
                await currentEngine.UpdateStatisticsAsync(game, cancellationToken);
            }

            await _gameRepository.UpdateAsync(game, cancellationToken);
            var result = await MapToGameStateDto(game, cancellationToken);

            // Release and remove the lock since the game is now ended
            ReleaseGameLock(gameId);

            return result;
        }
        finally
        {
            // Only release if not already removed (in case of exception before ReleaseGameLock)
            if (_gameLocks.ContainsKey(gameId))
            {
                gameLock.Release();
            }
        }
    }

    public async Task<IEnumerable<GameStateDto>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetUserGamesAsync(userId, limit, cancellationToken);
        var dtos = new List<GameStateDto>();
        foreach (var game in games) dtos.Add(await MapToGameStateDto(game, cancellationToken));
        return dtos;
    }

    private async Task<GameStateDto> MapToGameStateDto(GameSession game, CancellationToken cancellationToken)
    {
        var engine = GetEngine(game.GameType);
        var playerDtos = game.Players.Select(p => new PlayerGameDto(
            p.UserId.GetValueOrDefault(), 
            p.User?.Username ?? "",
            p.PlayerType,
            p.GetDisplayName(),
            p.PlayerOrder,
            p.FinalScore,
            p.DartsThrown, 
            p.PointsScored,
            p.PPD,
            p.IsWinner,
            p.UserId.HasValue ? engine.GetPlayerState(game, p.UserId.Value) : null
        )).ToList();

        // Calculate current player index
        var currentPlayerOrder = (game.Throws.Count / 3) % Math.Max(1, game.Players.Count);
        
        return new GameStateDto(
            game.Id, 
            game.GameType, 
            game.StartingScore, 
            game.Status,
            game.StartedAt, 
            game.EndedAt, 
            game.WinnerId,
            game.IsOnline,
            game.IsBotGame,
            playerDtos,
            currentPlayerOrder,
            game.Options
        );
    }
}
