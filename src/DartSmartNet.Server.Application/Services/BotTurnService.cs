using System.Runtime.CompilerServices;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Engines;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DartSmartNet.Server.Application.Services;

/// <summary>
/// Handles automatic bot turns for all game types
/// </summary>
public class BotTurnService : IBotTurnService
{
    private readonly IGameService _gameService;
    private readonly IBotService _botService;
    private readonly IEnumerable<IGameEngine> _engines;
    private readonly ILogger<BotTurnService> _logger;

    // Default bot difficulty when not specified
    private const BotDifficulty DefaultBotDifficulty = BotDifficulty.Medium;
    
    // Delay between bot throws for UI feedback (ms)
    private const int BotThrowDelayMs = 800;

    public BotTurnService(
        IGameService gameService,
        IBotService botService,
        IEnumerable<IGameEngine> engines,
        ILogger<BotTurnService> logger)
    {
        _gameService = gameService;
        _botService = botService;
        _engines = engines;
        _logger = logger;
    }

    public async Task<bool> ShouldBotPlayNextAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameService.GetGameEntityAsync(gameId, cancellationToken);
        if (game == null || game.Status != GameStatus.InProgress)
            return false;

        var nextPlayer = GetCurrentPlayer(game);
        return nextPlayer?.PlayerType == PlayerType.Bot;
    }

    public async IAsyncEnumerable<(GameStateDto State, int Segment, int Multiplier, int Points)> ExecuteBotTurnAsync(
        Guid gameId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var game = await _gameService.GetGameEntityAsync(gameId, cancellationToken);
        if (game == null || game.Status != GameStatus.InProgress)
            yield break;

        var botPlayer = GetCurrentPlayer(game);
        if (botPlayer?.PlayerType != PlayerType.Bot || !botPlayer.UserId.HasValue)
            yield break;

        var botUserId = botPlayer.UserId.Value;
        var engine = GetEngine(game.GameType);
        
        _logger.LogInformation("Bot {BotId} starting turn in game {GameId} (Type: {GameType})", 
            botUserId, gameId, game.GameType);

        // Execute up to 3 darts (or until game ends)
        for (int dart = 0; dart < 3; dart++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            // Re-fetch game state to get latest
            game = await _gameService.GetGameEntityAsync(gameId, cancellationToken);
            if (game == null || game.Status != GameStatus.InProgress)
                yield break;

            // Simulate the throw based on game type
            var score = await SimulateBotThrowAsync(game, botPlayer, engine, cancellationToken);
            
            _logger.LogDebug("Bot simulated throw: {Segment}x{Multiplier} = {Points}", 
                score.Segment, (int)score.Multiplier, score.Points);

            // Register the throw
            var gameState = await _gameService.RegisterThrowAsync(gameId, botUserId, score, null, cancellationToken);

            yield return (gameState, score.Segment, (int)score.Multiplier, score.Points);

            // Check if game ended (bot won or busted)
            if (gameState.Status != GameStatus.InProgress)
            {
                _logger.LogInformation("Game {GameId} ended during bot turn", gameId);
                yield break;
            }

            // Small delay between throws for visual effect
            await Task.Delay(BotThrowDelayMs, cancellationToken);
        }

        _logger.LogInformation("Bot {BotId} completed turn in game {GameId}", botUserId, gameId);
    }

    private async Task<Score> SimulateBotThrowAsync(
        GameSession game, 
        GamePlayer botPlayer, 
        IGameEngine engine,
        CancellationToken cancellationToken)
    {
        var botUserId = botPlayer.UserId!.Value;
        var difficulty = GetBotDifficulty(botPlayer);

        return game.GameType switch
        {
            GameType.X01 => await SimulateX01ThrowAsync(game, botUserId, difficulty, cancellationToken),
            GameType.Cricket => await SimulateCricketThrowAsync(difficulty, cancellationToken),
            GameType.AroundTheClock => await SimulateAtcThrowAsync(game, botUserId, engine, difficulty, cancellationToken),
            GameType.JdcChallenge => await SimulateJdcThrowAsync(game, botUserId, engine, difficulty, cancellationToken),
            _ => await _botService.SimulateThrowAsync(difficulty, 501, cancellationToken)
        };
    }

    private async Task<Score> SimulateX01ThrowAsync(
        GameSession game, 
        Guid botUserId,
        BotDifficulty difficulty,
        CancellationToken cancellationToken)
    {
        // Calculate current score for the bot
        var startingScore = game.StartingScore ?? 501;
        var pointsScored = game.Throws
            .Where(t => t.UserId == botUserId)
            .Sum(t => t.Points);
        var currentScore = startingScore - pointsScored;

        return await _botService.SimulateThrowAsync(difficulty, currentScore, cancellationToken);
    }

    private async Task<Score> SimulateCricketThrowAsync(
        BotDifficulty difficulty,
        CancellationToken cancellationToken)
    {
        // For Cricket, bot should aim for cricket numbers (20, 19, 18, 17, 16, 15, Bull)
        // Use X01 simulation with a pseudo score that encourages high number targeting
        return await _botService.SimulateThrowAsync(difficulty, 501, cancellationToken);
    }

    private async Task<Score> SimulateAtcThrowAsync(
        GameSession game,
        Guid botUserId,
        IGameEngine engine,
        BotDifficulty difficulty,
        CancellationToken cancellationToken)
    {
        // Get current target from player state
        var playerState = engine.GetPlayerState(game, botUserId);
        var currentTarget = playerState?.GetValueOrDefault(1, 1) ?? 1; // Key 1 = current target segment
        
        return await _botService.SimulateAtcThrowAsync(difficulty, currentTarget, cancellationToken);
    }

    private async Task<Score> SimulateJdcThrowAsync(
        GameSession game,
        Guid botUserId,
        IGameEngine engine,
        BotDifficulty difficulty,
        CancellationToken cancellationToken)
    {
        // Get JDC state from player state
        var playerState = engine.GetPlayerState(game, botUserId);
        var part = playerState?.GetValueOrDefault(0, 1) ?? 1;          // Key 0 = Part
        var currentTarget = playerState?.GetValueOrDefault(1, 10) ?? 10; // Key 1 = Current target
        
        // Part 2 is doubles-only
        var doublesOnly = part == 2;
        
        return await _botService.SimulateJdcThrowAsync(difficulty, currentTarget, doublesOnly, cancellationToken);
    }

    private GamePlayer? GetCurrentPlayer(GameSession game)
    {
        var throwCount = game.Throws.Count;
        var playerCount = game.Players.Count;
        if (playerCount == 0) return null;

        // Current player is determined by throw count divided by 3 (darts per round)
        var currentPlayerOrder = (throwCount / 3) % playerCount;
        return game.Players.OrderBy(p => p.PlayerOrder).ElementAtOrDefault(currentPlayerOrder);
    }

    private IGameEngine GetEngine(GameType type)
        => _engines.FirstOrDefault(e => e.GameType == type)
           ?? throw new InvalidOperationException($"No engine found for game type {type}");

    private static BotDifficulty GetBotDifficulty(GamePlayer botPlayer)
    {
        // Try to parse difficulty from display name (e.g., "Bot (Medium)")
        var displayName = botPlayer.DisplayName ?? "";
        
        if (displayName.Contains("Easy", StringComparison.OrdinalIgnoreCase))
            return BotDifficulty.Easy;
        if (displayName.Contains("Hard", StringComparison.OrdinalIgnoreCase))
            return BotDifficulty.Hard;
        if (displayName.Contains("Expert", StringComparison.OrdinalIgnoreCase))
            return BotDifficulty.Expert;
        
        return DefaultBotDifficulty;
    }
}
