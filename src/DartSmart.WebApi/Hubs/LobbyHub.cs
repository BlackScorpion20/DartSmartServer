using System.Security.Claims;
using DartSmart.Application.Interfaces;
using DartSmart.Application.Queries.Lobby;
using DartSmart.Application.Common;
using DartSmart.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DartSmart.WebApi.Hubs;

[Authorize]
public class LobbyHub : Hub
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IMediator _mediator;
    private static readonly Dictionary<string, string> _connectionPlayerMap = new();
    private static readonly object _lock = new();

    public LobbyHub(ILobbyRepository lobbyRepository, IMediator mediator)
    {
        _lobbyRepository = lobbyRepository;
        _mediator = mediator;
    }

    public async Task JoinLobby()
    {
        var playerIdStr = Context.User?.FindFirst("player_id")?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (string.IsNullOrEmpty(playerIdStr))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var playerId = PlayerId.From(Guid.Parse(playerIdStr));
        await _lobbyRepository.AddPlayerToLobbyAsync(playerId);
        
        lock (_lock)
        {
            _connectionPlayerMap[Context.ConnectionId] = playerIdStr;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "Lobby");

        await Clients.Group("Lobby").SendAsync("PlayerJoinedLobby", new
        {
            PlayerId = playerIdStr,
            Username = username,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveLobby()
    {
        var playerIdStr = Context.User?.FindFirst("player_id")?.Value;
        
        if (!string.IsNullOrEmpty(playerIdStr))
        {
            var playerId = PlayerId.From(Guid.Parse(playerIdStr));
            await _lobbyRepository.RemovePlayerFromLobbyAsync(playerId);
            
            lock (_lock)
            {
                _connectionPlayerMap.Remove(Context.ConnectionId);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Lobby");

            await Clients.Group("Lobby").SendAsync("PlayerLeftLobby", new
            {
                PlayerId = playerIdStr,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task GetMatches(decimal avgTolerance = 10)
    {
        var playerIdStr = Context.User?.FindFirst("player_id")?.Value;
        
        if (string.IsNullOrEmpty(playerIdStr))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var result = await _mediator.Send(new GetMatchingPlayersQuery(playerIdStr, avgTolerance));
        
        await result.Match<Task>(
            async matches =>
            {
                await Clients.Caller.SendAsync("MatchesFound", matches);
            },
            async error =>
            {
                await Clients.Caller.SendAsync("Error", error);
            }
        );
    }

    public async Task ChallengePlayer(string targetPlayerId)
    {
        var challengerIdStr = Context.User?.FindFirst("player_id")?.Value;
        var challengerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (string.IsNullOrEmpty(challengerIdStr))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Find the target player's connection
        string? targetConnectionId = null;
        lock (_lock)
        {
            targetConnectionId = _connectionPlayerMap
                .FirstOrDefault(x => x.Value == targetPlayerId).Key;
        }

        if (targetConnectionId != null)
        {
            await Clients.Client(targetConnectionId).SendAsync("ChallengeReceived", new
            {
                ChallengerId = challengerIdStr,
                ChallengerName = challengerName,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerIdStr = Context.User?.FindFirst("player_id")?.Value;
        
        if (!string.IsNullOrEmpty(playerIdStr))
        {
            var playerId = PlayerId.From(Guid.Parse(playerIdStr));
            await _lobbyRepository.RemovePlayerFromLobbyAsync(playerId);
            
            lock (_lock)
            {
                _connectionPlayerMap.Remove(Context.ConnectionId);
            }

            await Clients.Group("Lobby").SendAsync("PlayerLeftLobby", new
            {
                PlayerId = playerIdStr,
                Timestamp = DateTime.UtcNow
            });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
