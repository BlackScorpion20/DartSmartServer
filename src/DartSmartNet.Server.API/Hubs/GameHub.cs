using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DartSmartNet.Server.API.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly IBotTurnService _botTurnService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameService gameService, IBotTurnService botTurnService, ILogger<GameHub> logger)
    {
        _gameService = gameService;
        _botTurnService = botTurnService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected to GameHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected from GameHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        // Handle user disconnect - remove from SignalR groups but also notify active games
        // In a real app, we might want to flag the player as 'away' or handle reconnection
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a game session
    /// </summary>
    public async Task JoinGame(Guid gameId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} joining game {GameId}", userId, gameId);

            // Add user to SignalR group for this game
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGameGroupName(gameId));

            // Notify other players in the game
            await Clients.OthersInGroup(GetGameGroupName(gameId))
                .SendAsync("PlayerJoined", userId);

            _logger.LogInformation("User {UserId} successfully joined game {GameId}", userId, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game {GameId}", gameId);
            throw;
        }
    }

    /// <summary>
    /// Leave a game session
    /// </summary>
    public async Task LeaveGame(Guid gameId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} leaving game {GameId}", userId, gameId);

            // Remove user from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGameGroupName(gameId));

            // Notify other players
            await Clients.OthersInGroup(GetGameGroupName(gameId))
                .SendAsync("PlayerLeft", userId);

            _logger.LogInformation("User {UserId} left game {GameId}", userId, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving game {GameId}", gameId);
            throw;
        }
    }

    /// <summary>
    /// Register a dart throw in the game
    /// </summary>
    public async Task RegisterThrow(Guid gameId, int segment, int multiplier)
    {
        try
        {
            var userId = GetUserId();
            var username = GetUsername();
            _logger.LogInformation("User {UserId} registering throw in game {GameId}: {Segment}x{Multiplier}",
                userId, gameId, segment, multiplier);

            // Create Score from throw
            var score = CreateScore(segment, multiplier);
            var points = segment * multiplier;

            // Register throw via game service
            var gameState = await _gameService.RegisterThrowAsync(gameId, userId, score);

            // Calculate and broadcast incremental stats for all players
            var game = await _gameService.GetGameEntityAsync(gameId);
            if (game != null)
            {
                var incrementalStats = _gameService.CalculateIncrementalStats(game);
                await Clients.Group(GetGameGroupName(gameId))
                    .SendAsync("PlayerStatsUpdated", incrementalStats);

                _logger.LogInformation("Broadcasted incremental stats for game {GameId}", gameId);
            }

            // Broadcast updated game state to all players in the game
            await Clients.Group(GetGameGroupName(gameId))
                .SendAsync("GameStateUpdated", gameState);

            // Broadcast throw details to opponents for LED visualization
            var throwDetails = new OpponentThrowDto(
                userId,
                username,
                segment,
                multiplier,
                points,
                DateTime.UtcNow
            );

            await Clients.OthersInGroup(GetGameGroupName(gameId))
                .SendAsync("OpponentThrow", throwDetails);

            _logger.LogInformation("Throw registered successfully for game {GameId}", gameId);

            // After human throw, check if bot should play next
            await ExecuteBotTurnsIfNeededAsync(gameId, cancellationToken: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering throw for game {GameId}", gameId);

            // Send error to the caller only
            await Clients.Caller.SendAsync("ThrowError", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes bot turns automatically if the current player is a bot
    /// </summary>
    private async Task ExecuteBotTurnsIfNeededAsync(Guid gameId, CancellationToken cancellationToken)
    {
        try
        {
            // Keep executing bot turns while the current player is a bot
            while (await _botTurnService.ShouldBotPlayNextAsync(gameId, cancellationToken))
            {
                _logger.LogInformation("Executing bot turn for game {GameId}", gameId);

                await foreach (var (state, segment, multiplier, points) in 
                    _botTurnService.ExecuteBotTurnAsync(gameId, cancellationToken))
                {
                    // Broadcast the bot's throw to all players
                    var botThrowDetails = new OpponentThrowDto(
                        state.Players[state.CurrentPlayerIndex].UserId,
                        state.Players[state.CurrentPlayerIndex].DisplayName ?? "Bot",
                        segment,
                        multiplier,
                        points,
                        DateTime.UtcNow
                    );

                    await Clients.Group(GetGameGroupName(gameId))
                        .SendAsync("BotThrow", botThrowDetails);

                    // Broadcast updated game state
                    await Clients.Group(GetGameGroupName(gameId))
                        .SendAsync("GameStateUpdated", state);

                    // Calculate and broadcast incremental stats
                    var game = await _gameService.GetGameEntityAsync(gameId, cancellationToken);
                    if (game != null)
                    {
                        var incrementalStats = _gameService.CalculateIncrementalStats(game);
                        await Clients.Group(GetGameGroupName(gameId))
                            .SendAsync("PlayerStatsUpdated", incrementalStats);
                    }

                    // Check if game ended
                    if (state.Status != Domain.Enums.GameStatus.InProgress)
                    {
                        _logger.LogInformation("Game {GameId} ended during bot turn", gameId);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bot turn for game {GameId}", gameId);
            // Don't throw - bot turn failure shouldn't affect the human player's throw
        }
    }

    /// <summary>
    /// Notify opponents that a dart was removed (undo)
    /// </summary>
    public async Task NotifyDartRemoved(Guid gameId, int segment, int multiplier)
    {
        try
        {
            var userId = GetUserId();
            var username = GetUsername();

            var removeDetails = new OpponentThrowDto(
                userId,
                username,
                segment,
                multiplier,
                segment * multiplier,
                DateTime.UtcNow
            );
            
            await Clients.OthersInGroup(GetGameGroupName(gameId))
                .SendAsync("OpponentDartRemoved", removeDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying dart removed for game {GameId}", gameId);
        }
    }

    /// <summary>
    /// Notify when a round is completed (3 darts thrown)
    /// </summary>
    public async Task NotifyRoundComplete(Guid gameId, int totalPoints, int dartsThrown)
    {
        try
        {
            var userId = GetUserId();
            var username = GetUsername();

            var roundInfo = new RoundCompleteDto(
                userId,
                username,
                totalPoints,
                dartsThrown,
                DateTime.UtcNow
            );
            
            await Clients.OthersInGroup(GetGameGroupName(gameId))
                .SendAsync("OpponentRoundComplete", roundInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying round complete for game {GameId}", gameId);
        }
    }

    /// <summary>
    /// Start the game
    /// </summary>
    public async Task StartGame(Guid gameId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} starting game {GameId}", userId, gameId);

            var gameState = await _gameService.StartGameAsync(gameId);

            // Notify all players that game has started
            await Clients.Group(GetGameGroupName(gameId))
                .SendAsync("GameStarted", gameState);

            _logger.LogInformation("Game {GameId} started successfully", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game {GameId}", gameId);
            await Clients.Caller.SendAsync("StartGameError", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// End the game
    /// </summary>
    public async Task EndGame(Guid gameId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} ending game {GameId}", userId, gameId);

            var gameState = await _gameService.EndGameAsync(gameId);

            // Notify all players that game has ended
            await Clients.Group(GetGameGroupName(gameId))
                .SendAsync("GameEnded", gameState);

            _logger.LogInformation("Game {GameId} ended successfully", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending game {GameId}", gameId);
            await Clients.Caller.SendAsync("EndGameError", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Send a chat message to all players in the game
    /// </summary>
    public async Task SendMessage(Guid gameId, string message)
    {
        try
        {
            var userId = GetUserId();
            var username = GetUsername();

            _logger.LogInformation("User {UserId} sending message to game {GameId}", userId, gameId);

            await Clients.Group(GetGameGroupName(gameId))
                .SendAsync("MessageReceived", new
                {
                    UserId = userId,
                    Username = username,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to game {GameId}", gameId);
            throw;
        }
    }

    /// <summary>
    /// Request current game state
    /// </summary>
    public async Task GetGameState(Guid gameId)
    {
        try
        {
            var gameState = await _gameService.GetGameStateAsync(gameId);

            // Send game state to caller only
            await Clients.Caller.SendAsync("GameStateUpdated", gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state for game {GameId}", gameId);
            await Clients.Caller.SendAsync("GetGameStateError", ex.Message);
            throw;
        }
    }

    // Helper methods

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("User ID not found in claims");
        }

        return userId;
    }

    private string GetUsername()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? Context.User?.FindFirst("unique_name")?.Value
            ?? "Unknown";
    }

    private static string GetGameGroupName(Guid gameId)
    {
        return $"game_{gameId}";
    }

    private static Score CreateScore(int segment, int multiplier)
    {
        return multiplier switch
        {
            1 => Score.Single(segment),
            2 when segment == 25 => Score.DoubleBull(),
            2 => Score.Double(segment),
            3 => Score.Triple(segment),
            _ => Score.Miss()
        };
    }
}

/// <summary>
/// DTO for broadcasting opponent throws to enable LED visualization
/// </summary>
public record OpponentThrowDto(
    Guid PlayerId,
    string PlayerName,
    int Segment,
    int Multiplier,
    int Points,
    DateTime Timestamp
);

/// <summary>
/// DTO for broadcasting when opponent completes their round
/// </summary>
public record RoundCompleteDto(
    Guid PlayerId,
    string PlayerName,
    int TotalPoints,
    int DartsThrown,
    DateTime Timestamp
);
