using System.Security.Claims;
using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Application.Queries.Lobby;
using DartSmart.Domain.Common;

namespace DartSmart.WebApi.Endpoints;

public static class LobbyEndpoints
{
    public static void MapLobbyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lobby").WithTags("Matchmaking");

        group.MapGet("/matches", async (
            decimal avgTolerance,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var playerId = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new GetMatchingPlayersQuery(playerId, avgTolerance));
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .RequireAuthorization()
        .WithName("GetMatchingPlayers")
        .Produces<List<LobbyPlayerDto>>(200);

        group.MapPost("/join", async (ClaimsPrincipal user, ILobbyRepository lobbyRepository) =>
        {
            var playerIdStr = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerIdStr))
                return Results.Unauthorized();

            var playerId = PlayerId.From(Guid.Parse(playerIdStr));
            await lobbyRepository.AddPlayerToLobbyAsync(playerId);
            
            return Results.Ok(new { message = "Joined lobby" });
        })
        .RequireAuthorization()
        .WithName("JoinLobby")
        .Produces(200);

        group.MapPost("/leave", async (ClaimsPrincipal user, ILobbyRepository lobbyRepository) =>
        {
            var playerIdStr = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerIdStr))
                return Results.Unauthorized();

            var playerId = PlayerId.From(Guid.Parse(playerIdStr));
            await lobbyRepository.RemovePlayerFromLobbyAsync(playerId);
            
            return Results.Ok(new { message = "Left lobby" });
        })
        .RequireAuthorization()
        .WithName("LeaveLobby")
        .Produces(200);
    }
}
