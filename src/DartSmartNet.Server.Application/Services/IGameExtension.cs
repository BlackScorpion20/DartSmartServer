using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Events;

namespace DartSmartNet.Server.Application.Services;

/// <summary>
/// Interface for game extensions that can listen to and react to game events
/// </summary>
public interface IGameExtension
{
    /// <summary>
    /// Unique identifier for this extension
    /// </summary>
    string ExtensionId { get; }

    /// <summary>
    /// Display name for this extension
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called when a game event occurs
    /// </summary>
    Task OnGameEventAsync(GameEvent gameEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this extension is enabled for a specific game
    /// </summary>
    Task<bool> IsEnabledForGameAsync(Guid gameId, CancellationToken cancellationToken = default);
}
