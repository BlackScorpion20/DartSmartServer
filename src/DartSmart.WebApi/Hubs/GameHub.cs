using System.Security.Claims;
using DartSmart.Application.Commands.Game;
using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DartSmart.WebApi.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IMediator _mediator;

    public GameHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        
        var playerId = Context.User?.FindFirst("player_id")?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        
        await Clients.Group(gameId).SendAsync("PlayerJoined", new
        {
            PlayerId = playerId,
            Username = username,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        
        var playerId = Context.User?.FindFirst("player_id")?.Value;
        await Clients.Group(gameId).SendAsync("PlayerLeft", new
        {
            PlayerId = playerId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task ThrowDart(string gameId, int segment, int multiplier, int dartNumber)
    {
        var playerId = Context.User?.FindFirst("player_id")?.Value;
        if (string.IsNullOrEmpty(playerId))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var command = new RegisterThrowCommand(playerId, gameId, segment, multiplier, dartNumber);
        var result = await _mediator.Send(command);

        await result.Match<Task>(
            async success =>
            {
                await Clients.Group(gameId).SendAsync("ThrowRegistered", success);
                
                // Check if all 3 darts thrown
                if (dartNumber == 3)
                {
                    await Clients.Group(gameId).SendAsync("TurnComplete", new
                    {
                        PlayerId = playerId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            },
            async error =>
            {
                await Clients.Caller.SendAsync("ThrowError", new { Error = error });
            }
        );
    }

    public async Task EndTurn(string gameId)
    {
        var playerId = Context.User?.FindFirst("player_id")?.Value;
        
        await Clients.Group(gameId).SendAsync("TurnEnded", new
        {
            PlayerId = playerId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task StartGame(string gameId)
    {
        var command = new StartGameCommand(gameId);
        var result = await _mediator.Send(command);

        await result.Match<Task>(
            async success =>
            {
                await Clients.Group(gameId).SendAsync("GameStarted", success);
            },
            async error =>
            {
                await Clients.Caller.SendAsync("Error", error);
            }
        );
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerId = Context.User?.FindFirst("player_id")?.Value;
        // Could notify all groups the player was in
        await base.OnDisconnectedAsync(exception);
    }
}
