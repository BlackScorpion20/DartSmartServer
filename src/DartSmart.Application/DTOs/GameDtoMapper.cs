using DartSmart.Domain.Entities;

namespace DartSmart.Application.DTOs;

/// <summary>
/// Mapper for Game to GameDto conversion
/// </summary>
public static class GameDtoMapper
{
    public static GameDto Map(Game game, IEnumerable<Player> players)
    {
        var playerLookup = players.ToDictionary(p => p.Id.Value.ToString());
        
        return new GameDto(
            game.Id.Value.ToString(),
            game.GameType.ToString(),
            game.StartScore,
            game.InMode.ToString(),
            game.OutMode.ToString(),
            game.Status.ToString(),
            game.CreatedAt,
            game.FinishedAt,
            game.WinnerId?.Value.ToString(),
            game.CurrentPlayerIndex,
            game.CurrentRound,
            game.Players.Select(gp => new GamePlayerDto(
                gp.PlayerId.Value.ToString(),
                playerLookup.TryGetValue(gp.PlayerId.Value.ToString(), out var p) ? p.Username : "Unknown",
                gp.CurrentScore,
                gp.TurnOrder,
                gp.DartsThrown,
                gp.LegsWon
            )).ToList()
        );
    }
}
