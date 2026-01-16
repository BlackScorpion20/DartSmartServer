using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Events;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DartSmartNet.Server.Infrastructure.Extensions;

/// <summary>
/// Extension that persists all game events to the database for replay and analysis
/// </summary>
public class EventLoggingExtension : IGameExtension
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EventLoggingExtension> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ExtensionId => "event-logger";
    public string Name => "Event Logger";

    public EventLoggingExtension(
        ApplicationDbContext dbContext,
        ILogger<EventLoggingExtension> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task OnGameEventAsync(GameEvent gameEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventData = JsonSerializer.Serialize(gameEvent, _jsonOptions);
            var playerUsername = ExtractPlayerUsername(gameEvent);

            var eventLog = new GameEventLog(
                gameEvent.GameId,
                gameEvent.EventType,
                eventData,
                playerUsername
            );

            _dbContext.Set<GameEventLog>().Add(eventLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Logged event {EventType} for game {GameId}",
                gameEvent.EventType, gameEvent.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging event {EventType} for game {GameId}",
                gameEvent.EventType, gameEvent.GameId);
        }
    }

    public Task<bool> IsEnabledForGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // Event logging is always enabled
        return Task.FromResult(true);
    }

    private static string? ExtractPlayerUsername(GameEvent gameEvent)
    {
        return gameEvent switch
        {
            DartsThrowEvent throwEvent => throwEvent.Player,
            DartsPulledEvent pulledEvent => pulledEvent.Player,
            BustedEvent bustedEvent => bustedEvent.Player,
            GameWonEvent wonEvent => wonEvent.Player,
            PlayerChangedEvent changedEvent => changedEvent.Player,
            _ => null
        };
    }
}
