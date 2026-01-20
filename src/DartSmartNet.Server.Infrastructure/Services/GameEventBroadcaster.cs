using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DartSmartNet.Server.Infrastructure.Services;

/// <summary>
/// Broadcasts game events to all registered extensions
/// </summary>
public class GameEventBroadcaster : IGameEventBroadcaster
{
    private readonly IEnumerable<IGameExtension> _extensions;
    private readonly ILogger<GameEventBroadcaster> _logger;

    public GameEventBroadcaster(IEnumerable<IGameExtension> extensions, ILogger<GameEventBroadcaster> logger)
    {
        _extensions = extensions;
        _logger = logger;
    }

    public void RegisterExtension(IGameExtension extension)
    {
        // No-op or throw if manual registration is no longer desired
        _logger.LogWarning("Manual registration of extensions is deprecated. Register via DI instead.");
    }

    public void UnregisterExtension(string extensionId)
    {
        _logger.LogWarning("Manual unregistration of extensions is not supported in DI mode.");
    }

    public IReadOnlyList<IGameExtension> GetRegisteredExtensions()
    {
        return _extensions.ToList().AsReadOnly();
    }

    public async Task BroadcastEventAsync(GameEvent gameEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting event {EventType} for game {GameId}",
            gameEvent.EventType, gameEvent.GameId);

        var tasks = new List<Task>();

        foreach (var extension in _extensions)
        {
            tasks.Add(BroadcastToExtensionAsync(extension, gameEvent, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task BroadcastToExtensionAsync(
        IGameExtension extension,
        GameEvent gameEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if extension is enabled for this game
            if (!await extension.IsEnabledForGameAsync(gameEvent.GameId, cancellationToken))
            {
                return;
            }

            await extension.OnGameEventAsync(gameEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error broadcasting event {EventType} to extension {ExtensionId}",
                gameEvent.EventType, extension.ExtensionId);
        }
    }
}
