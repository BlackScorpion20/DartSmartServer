using DartSmartNet.Server.Application.DTOs.Stats;
using DartSmartNet.Server.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(IStatisticsService statisticsService, ILogger<StatsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get statistics for the current user
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PlayerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyStats(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} retrieving their statistics", userId);

            var stats = await _statisticsService.GetUserStatsAsync(userId, cancellationToken);

            if (stats == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Statistics not found",
                    Detail = "No statistics found for the current user"
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving statistics"
            });
        }
    }

    /// <summary>
    /// Get statistics for a specific user by ID
    /// </summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(PlayerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStats(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving statistics for user {UserId}", userId);

            var stats = await _statisticsService.GetUserStatsAsync(userId, cancellationToken);

            if (stats == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Statistics not found",
                    Detail = $"No statistics found for user {userId}"
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving statistics"
            });
        }
    }

    /// <summary>
    /// Get leaderboard with top players
    /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(IEnumerable<PlayerStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving leaderboard with limit {Limit}", limit);

            var leaderboard = await _statisticsService.GetLeaderboardAsync(limit, cancellationToken);

            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving leaderboard"
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
