using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Events;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DartSmartNet.Server.Infrastructure.Extensions;

/// <summary>
/// Extension that persists all game events to the database for replay and analysis
/// </summary>
public class EventLoggingExtension : IGameExtension
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventLoggingExtension> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ExtensionId => "event-logger";
    public string Name => "Event Logger";

    public EventLoggingExtension(
        IServiceScopeFactory scopeFactory,
        ILogger<EventLoggingExtension> logger)
    {
        _scopeFactory = scopeFactory;
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
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var eventData = JsonSerializer.Serialize(gameEvent, _jsonOptions);
            var playerUsername = ExtractPlayerUsername(gameEvent);

            var eventLog = new GameEventLog(
                gameEvent.GameId,
                gameEvent.EventType,
                eventData,
                playerUsername
            );

            dbContext.Set<GameEventLog>().Add(eventLog);
            await dbContext.SaveChangesAsync(cancellationToken);

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
