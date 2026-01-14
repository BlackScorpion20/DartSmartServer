using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BotController : ControllerBase
{
    private readonly IBotRepository _botRepository;
    private readonly ILogger<BotController> _logger;

    public BotController(IBotRepository botRepository, ILogger<BotController> logger)
    {
        _botRepository = botRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all available bots
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBots(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving all bots");

            var bots = await _botRepository.GetAllAsync(cancellationToken);
            var botDtos = bots.Select(MapToDto);

            return Ok(botDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all bots");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving bots"
            });
        }
    }

    /// <summary>
    /// Get bot by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBot(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving bot {BotId}", id);

            var bot = await _botRepository.GetByIdAsync(id, cancellationToken);

            if (bot == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Bot not found",
                    Detail = $"Bot with ID {id} was not found"
                });
            }

            return Ok(MapToDto(bot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot {BotId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving bot"
            });
        }
    }

    /// <summary>
    /// Get bot by difficulty level
    /// </summary>
    [HttpGet("difficulty/{difficulty}")]
    [ProducesResponseType(typeof(BotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBotByDifficulty(BotDifficulty difficulty, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving bot with difficulty {Difficulty}", difficulty);

            var bot = await _botRepository.GetByDifficultyAsync(difficulty, cancellationToken);

            if (bot == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Bot not found",
                    Detail = $"No bot found with difficulty {difficulty}"
                });
            }

            return Ok(MapToDto(bot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot by difficulty {Difficulty}", difficulty);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred while retrieving bot"
            });
        }
    }

    private static BotDto MapToDto(Bot bot)
    {
        return new BotDto(
            bot.Id,
            bot.Name,
            bot.Difficulty,
            bot.AvgPPD,
            bot.ConsistencyFactor,
            bot.CheckoutSkill
        );
    }
}

public record BotDto(
    Guid Id,
    string Name,
    BotDifficulty Difficulty,
    decimal AvgPPD,
    decimal ConsistencyFactor,
    decimal CheckoutSkill
);
