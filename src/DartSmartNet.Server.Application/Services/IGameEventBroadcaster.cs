using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Events;

namespace DartSmartNet.Server.Application.Services;

/// <summary>
/// Service for broadcasting game events to extensions and external clients
/// </summary>
public interface IGameEventBroadcaster
{
    /// <summary>
    /// Broadcast a game event to all registered extensions and external listeners
    /// </summary>
    Task BroadcastEventAsync(GameEvent gameEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register an extension to receive game events
    /// </summary>
    void RegisterExtension(IGameExtension extension);

    /// <summary>
    /// Unregister an extension
    /// </summary>
    void UnregisterExtension(string extensionId);

    /// <summary>
    /// Get all registered extensions
    /// </summary>
    IReadOnlyList<IGameExtension> GetRegisteredExtensions();
}
