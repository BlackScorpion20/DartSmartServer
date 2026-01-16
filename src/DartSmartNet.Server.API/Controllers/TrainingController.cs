using System;
using System.Threading;
using System.Threading.Tasks;
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
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;

    public TrainingController(ITrainingService trainingService)
    {
        _trainingService = trainingService;
    }

    /// <summary>
    /// Start a new training session
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartTraining(
        [FromBody] StartTrainingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            var session = await _trainingService.StartTrainingAsync(
                userId,
                request.TrainingType,
                cancellationToken);

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to start training",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get current training session state
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetTrainingSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _trainingService.GetTrainingSessionAsync(sessionId, cancellationToken);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Training session not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Register a throw in a training session
    /// </summary>
    [HttpPost("{sessionId}/throw")]
    public async Task<IActionResult> RegisterThrow(
        Guid sessionId,
        [FromBody] RegisterThrowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var score = request.Multiplier switch
            {
                Multiplier.Single => Score.Single(request.Segment),
                Multiplier.Double => Score.Double(request.Segment),
                Multiplier.Triple => Score.Triple(request.Segment),
                _ => Score.Miss()
            };

            var session = await _trainingService.RegisterTrainingThrowAsync(
                sessionId,
                score,
                cancellationToken);

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to register throw",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// End a training session
    /// </summary>
    [HttpPost("{sessionId}/end")]
    public async Task<IActionResult> EndTraining(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _trainingService.EndTrainingAsync(sessionId, cancellationToken);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to end training",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get training history for the current user
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetTrainingHistory(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        var history = await _trainingService.GetUserTrainingHistoryAsync(
            userId,
            limit,
            cancellationToken);

        return Ok(history);
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

public record StartTrainingRequest(GameType TrainingType);

public record RegisterThrowRequest(int Segment, Multiplier Multiplier);
