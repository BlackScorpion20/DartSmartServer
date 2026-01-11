using System.Security.Claims;
using DartSmart.Application.Commands.Game;
using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Queries.Games;
using DartSmart.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace DartSmart.WebApi.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games").WithTags("Games");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetActiveGamesQuery());
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .WithName("GetActiveGames")
        .Produces<List<GameSummaryDto>>(200);

        group.MapPost("/", async (CreateGameDto request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var playerId = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerId))
                return Results.Unauthorized();

            var command = new CreateGameCommand(
                playerId,
                request.GameType,
                request.StartScore,
                request.InMode,
                request.OutMode
            );

            var result = await mediator.Send(command);
            return result.Match(
                success => Results.Created($"/api/games/{success.Id}", success),
                error => Results.BadRequest(new { error })
            );
        })
        .RequireAuthorization()
        .WithName("CreateGame")
        .Produces<GameDto>(201)
        .Produces(400)
        .Produces(401);

        group.MapPost("/{gameId}/join", async (string gameId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var playerId = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerId))
                return Results.Unauthorized();

            var command = new JoinGameCommand(playerId, gameId);
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .RequireAuthorization()
        .WithName("JoinGame")
        .Produces<GameDto>(200)
        .Produces(400);

        group.MapPost("/{gameId}/start", async (string gameId, IMediator mediator) =>
        {
            var command = new StartGameCommand(gameId);
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .RequireAuthorization()
        .WithName("StartGame")
        .Produces<GameDto>(200)
        .Produces(400);

        group.MapPost("/{gameId}/throw", async (
            string gameId, 
            [FromBody] RegisterThrowDto request, 
            ClaimsPrincipal user, 
            IMediator mediator) =>
        {
            var playerId = user.FindFirst("player_id")?.Value;
            if (string.IsNullOrEmpty(playerId))
                return Results.Unauthorized();

            var command = new RegisterThrowCommand(
                playerId,
                gameId,
                request.Segment,
                request.Multiplier,
                request.DartNumber
            );

            var result = await mediator.Send(command);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .RequireAuthorization()
        .WithName("RegisterThrow")
        .Produces<DartThrowDto>(200)
        .Produces(400);
    }
}
