using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartSmartNet.Server.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly IMatchmakingService _matchmakingService;
    private readonly ILogger<MatchmakingController> _logger;

    public MatchmakingController(IMatchmakingService matchmakingService, ILogger<MatchmakingController> logger)
    {
        _matchmakingService = matchmakingService;
        _logger = logger;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinQueue([FromBody] JoinQueueRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (!Enum.TryParse<GameType>(request.GameType, true, out var gameType))
        {
            return BadRequest("Invalid game type.");
        }

        _logger.LogInformation("User {UserId} joining matchmaking queue for {GameType}", userId, gameType);

        var gameId = await _matchmakingService.JoinQueueAsync(userId, gameType, request.StartingScore);

        return Ok(new JoinQueueResponse(gameId, gameId == null));
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveQueue()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("User {UserId} leaving matchmaking queue", userId);

        await _matchmakingService.LeaveQueueAsync(userId);

        return Ok();
    }

    [HttpGet("status")]
    public async Task<ActionResult<MatchmakingStatusDto>> GetStatus()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var status = await _matchmakingService.GetQueueStatusAsync(userId);
        return Ok(status);
    }

    [HttpGet("count/{gameType}")]
    public async Task<ActionResult<int>> GetQueueCount(string gameType)
    {
        if (!Enum.TryParse<GameType>(gameType, true, out var gType))
        {
            return BadRequest("Invalid game type.");
        }

        var count = await _matchmakingService.GetQueueCountAsync(gType);
        return Ok(count);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

public record JoinQueueRequest(string GameType, int? StartingScore);
public record JoinQueueResponse(Guid? GameId, bool InQueue);
