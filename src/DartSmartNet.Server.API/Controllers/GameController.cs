using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ILogger<GameController> _logger;

    public GameController(IGameService gameService, ILogger<GameController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new game session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} creating game: {GameType}", userId, request.GameType);

            GameStateDto gameState;

            // Check if new Players format is provided
            if (request.Players != null && request.Players.Count > 0)
            {
                // Use new format with PlayerSetup
                var players = request.Players
                    .Select(p => new PlayerSetupDto(p.UserId, p.PlayerType, p.DisplayName))
                    .ToList();

                // Ensure current user is included
                if (!players.Any(p => p.UserId == userId))
                {
                    players.Insert(0, new PlayerSetupDto(userId, PlayerType.Human, null));
                }

                gameState = await _gameService.CreateGameAsync(
                    request.GameType,
                    request.StartingScore,
                    players.ToArray(),
                    request.IsOnline,
                    request.Options,
                    cancellationToken);
            }
            else
            {
                // Legacy format with PlayerIds
                var playerIds = request.PlayerIds?.ToList() ?? new List<Guid>();
                if (!playerIds.Contains(userId))
                {
                    playerIds.Insert(0, userId);
                }

                gameState = await _gameService.CreateGameAsync(
                    request.GameType,
                    request.StartingScore,
                    playerIds.ToArray(),
                    request.IsOnline,
                    request.Options,
                    cancellationToken);
            }

            _logger.LogInformation("Game {GameId} created successfully", gameState.GameId);

            return Ok(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to create game",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get game state by ID
    /// </summary>
    [HttpGet("{gameId}")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(Guid gameId, CancellationToken cancellationToken)
    {
        try
        {
            var gameState = await _gameService.GetGameStateAsync(gameId, cancellationToken);

            if (gameState == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Game not found",
                    Detail = $"Game with ID {gameId} was not found"
                });
            }

            return Ok(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game {GameId}", gameId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving game"
            });
        }
    }

    /// <summary>
    /// Get all games for the current user
    /// </summary>
    [HttpGet("my-games")]
    [ProducesResponseType(typeof(IEnumerable<GameStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGames([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} retrieving games with limit {Limit}", userId, limit);

            var games = await _gameService.GetUserGamesAsync(userId, limit, cancellationToken);

            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user games");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving games"
            });
        }
    }

    /// <summary>
    /// Start a game
    /// </summary>
    [HttpPost("{gameId}/start")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartGame(Guid gameId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} starting game {GameId}", userId, gameId);

            var gameState = await _gameService.StartGameAsync(gameId, cancellationToken);

            return Ok(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game {GameId}", gameId);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to start game",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Register a throw in a game
    /// </summary>
    [HttpPost("{gameId}/throw")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterThrow(Guid gameId, [FromBody] RegisterGameThrowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} registering throw in game {GameId}: {Segment} x{Multiplier}",
                userId, gameId, request.Score.Segment, request.Score.Multiplier);

            var gameState = await _gameService.RegisterThrowAsync(gameId, userId, request.Score, request.RawData, cancellationToken);

            return Ok(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering throw in game {GameId}", gameId);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to register throw",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// End a game
    /// </summary>
    [HttpPost("{gameId}/end")]
    [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndGame(Guid gameId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} ending game {GameId}", userId, gameId);

            var gameState = await _gameService.EndGameAsync(gameId, cancellationToken);

            return Ok(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending game {GameId}", gameId);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to end game",
                Detail = ex.Message
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

/// <summary>
/// Player setup for creating a new game
/// </summary>
public record PlayerSetup(
    Guid? UserId,
    PlayerType PlayerType,
    string? DisplayName = null
);

public record CreateGameRequest(
    GameType GameType,
    int? StartingScore,
    /// <summary>
    /// Legacy: Simple array of player IDs (all treated as Human, Guid.Empty as Bot)
    /// </summary>
    Guid[]? PlayerIds = null,
    /// <summary>
    /// New: Full player setup with type and display name
    /// </summary>
    List<PlayerSetup>? Players = null,
    bool IsOnline = false,
    GameOptions? Options = null
);

public record RegisterGameThrowRequest(
    Score Score,
    byte[]? RawData = null
);

