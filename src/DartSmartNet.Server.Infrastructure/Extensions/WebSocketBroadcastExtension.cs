using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DartSmartNet.Server.Infrastructure.Extensions;

/// <summary>
/// Extension that broadcasts game events via SignalR to external clients
/// This enables third-party integrations (LED controllers, overlays, etc.)
/// </summary>
public class WebSocketBroadcastExtension : IGameExtension
{
    private readonly IHubContext<BroadcastHub> _hubContext;
    private readonly ILogger<WebSocketBroadcastExtension> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ExtensionId => "websocket-broadcast";
    public string Name => "WebSocket Broadcaster";

    public WebSocketBroadcastExtension(
        IHubContext<BroadcastHub> hubContext,
        ILogger<WebSocketBroadcastExtension> logger)
    {
        _hubContext = hubContext;
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
            // Broadcast to all connected clients on the broadcast hub
            await _hubContext.Clients.All.SendAsync(
                "GameEvent",
                gameEvent,
                cancellationToken);

            // Also send to game-specific group if anyone is listening
            var gameGroup = $"broadcast_game_{gameEvent.GameId}";
            await _hubContext.Clients.Group(gameGroup).SendAsync(
                "GameEvent",
                gameEvent,
                cancellationToken);

            _logger.LogDebug("Broadcasted event {EventType} for game {GameId}",
                gameEvent.EventType, gameEvent.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting event {EventType}",
                gameEvent.EventType);
        }
    }

    public Task<bool> IsEnabledForGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        // WebSocket broadcasting is always enabled
        return Task.FromResult(true);
    }
}

/// <summary>
/// SignalR Hub for broadcasting game events to external clients
/// </summary>
public class BroadcastHub : Hub
{
    private readonly ILogger<BroadcastHub> _logger;

    public BroadcastHub(ILogger<BroadcastHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to BroadcastHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from BroadcastHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to events for a specific game
    /// </summary>
    public async Task SubscribeToGame(Guid gameId)
    {
        var groupName = $"broadcast_game_{gameId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} subscribed to game {GameId}",
            Context.ConnectionId, gameId);
    }

    /// <summary>
    /// Unsubscribe from game events
    /// </summary>
    public async Task UnsubscribeFromGame(Guid gameId)
    {
        var groupName = $"broadcast_game_{gameId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from game {GameId}",
            Context.ConnectionId, gameId);
    }
}
