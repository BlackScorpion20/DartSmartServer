using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Queries.Players;

namespace DartSmart.WebApi.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stats").WithTags("Statistics");

        group.MapGet("/player/{playerId}", async (string playerId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPlayerStatisticsQuery(playerId));
            return result.Match(
                success => Results.Ok(success),
                error => Results.NotFound(new { error })
            );
        })
        .WithName("GetPlayerStatistics")
        .Produces<PlayerStatisticsDto>(200)
        .Produces(404);

        group.MapGet("/leaderboard", async (
            LeaderboardType type,
            int count,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new GetLeaderboardQuery(type, count));
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .WithName("GetLeaderboard")
        .Produces<List<LeaderboardEntryDto>>(200);
    }
}
