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
    private readonly List<IGameExtension> _extensions = new();
    private readonly ILogger<GameEventBroadcaster> _logger;

    public GameEventBroadcaster(ILogger<GameEventBroadcaster> logger)
    {
        _logger = logger;
    }

    public void RegisterExtension(IGameExtension extension)
    {
        if (_extensions.Any(e => e.ExtensionId == extension.ExtensionId))
        {
            _logger.LogWarning("Extension {ExtensionId} is already registered", extension.ExtensionId);
            return;
        }

        _extensions.Add(extension);
        _logger.LogInformation("Registered extension: {ExtensionName} ({ExtensionId})",
            extension.Name, extension.ExtensionId);
    }

    public void UnregisterExtension(string extensionId)
    {
        var removed = _extensions.RemoveAll(e => e.ExtensionId == extensionId);
        if (removed > 0)
        {
            _logger.LogInformation("Unregistered extension: {ExtensionId}", extensionId);
        }
    }

    public IReadOnlyList<IGameExtension> GetRegisteredExtensions()
    {
        return _extensions.AsReadOnly();
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
