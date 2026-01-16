using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DartSmartNet.Server.Infrastructure.Extensions;

/// <summary>
/// Extension that tracks and updates player statistics based on game events
/// </summary>
public class StatisticsExtension : IGameExtension
{
    private readonly IStatsRepository _statsRepository;
    private readonly ILogger<StatisticsExtension> _logger;

    public string ExtensionId => "statistics-tracker";
    public string Name => "Statistics Tracker";

    public StatisticsExtension(
        IStatsRepository statsRepository,
        ILogger<StatisticsExtension> logger)
    {
        _statsRepository = statsRepository;
        _logger = logger;
    }

    public async Task OnGameEventAsync(GameEvent gameEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (gameEvent)
            {
                case GameWonEvent wonEvent:
                    await HandleGameWonAsync(wonEvent, cancellationToken);
                    break;

                case DartsThrowEvent throwEvent:
                    await HandleDartsThrowAsync(throwEvent, cancellationToken);
                    break;

                // Add more event handlers as needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventType} in StatisticsExtension",
                gameEvent.EventType);
        }
    }

    public Task<bool> IsEnabledForGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // Statistics tracking is always enabled
        return Task.FromResult(true);
    }

    private async Task HandleGameWonAsync(GameWonEvent wonEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating statistics for game won by {Player}", wonEvent.Player);

        // Update winner stats
        // This would integrate with your existing statistics service
        // For now, just log it
        _logger.LogInformation(
            "Player {Player} won game {GameId} with {PPD} PPD in {Darts} darts",
            wonEvent.Player, wonEvent.GameId, wonEvent.AveragePPD, wonEvent.DartsThrown);
    }

    private async Task HandleDartsThrowAsync(DartsThrowEvent throwEvent, CancellationToken cancellationToken)
    {
        // Track special throws (180s, high checkouts, etc.)
        if (throwEvent.Points == 180)
        {
            _logger.LogInformation("180 scored by {Player} in game {GameId}",
                throwEvent.Player, throwEvent.GameId);
        }
    }
}
