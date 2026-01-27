using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Engines;

public class CricketEngine : IGameEngine
{
    private readonly IStatisticsService _statisticsService;
    private static readonly int[] CricketSegments = { 20, 19, 18, 17, 16, 15, 25 };

    public GameType GameType => GameType.Cricket;

    public CricketEngine(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public int CalculateCurrentScore(GameSession game, Guid userId)
    {
        var states = CalculateCricketState(game);
        return states.ContainsKey(userId) ? states[userId].Score : 0;
    }

    public bool CheckWinCondition(GameSession game, Guid userId, out int? finalScore)
    {
        var states = CalculateCricketState(game);
        if (!states.ContainsKey(userId))
        {
            finalScore = null;
            return false;
        }

        var playerState = states[userId];
        var allClosed = CricketSegments.All(s => playerState.Marks.ContainsKey(s) && playerState.Marks[s] >= 3);
        var isHighestScore = states.All(kv => kv.Value.Score <= playerState.Score);

        if (allClosed && isHighestScore)
        {
            finalScore = playerState.Score;
            return true;
        }

        finalScore = null;
        return false;
    }

    public async Task UpdateStatisticsAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        var states = CalculateCricketState(game);

        foreach (var player in game.Players)
        {
            if (!player.UserId.HasValue) continue;  // Skip bot players

            var playerScore = states.ContainsKey(player.UserId.Value) ? states[player.UserId.Value].Score : 0;
            var totalDarts = game.Throws.Count(t => t.UserId == player.UserId.Value);

            // Update the GamePlayer entity
            player.OverrideStats(playerScore, totalDarts);

            var roundScoresList = new List<int>();

            var playerMarks = states.ContainsKey(player.UserId.Value) ? states[player.UserId.Value].Marks.Values.Sum() : 0;

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId.Value,
                player.IsWinner,
                totalDarts,
                playerScore,
                roundScoresList,
                0,
                playerMarks,
                cancellationToken);
        }
    }

    public Dictionary<int, int>? GetPlayerState(GameSession game, Guid userId)
    {
        var states = CalculateCricketState(game);
        return states.ContainsKey(userId) ? states[userId].Marks : null;
    }

    public Dictionary<Guid, (int Score, Dictionary<int, int> Marks)> CalculateCricketState(GameSession game)
    {
        var state = game.Players
            .Where(p => p.UserId.HasValue)
            .ToDictionary(
                p => p.UserId!.Value,
                p => (Score: 0, Marks: CricketSegments.ToDictionary(s => s, s => 0))
        );

        var sortedThrows = game.Throws.OrderBy(t => t.ThrownAt).ToList();

        foreach (var t in sortedThrows)
        {
            if (!CricketSegments.Contains(t.Segment)) continue;

            var playerId = t.UserId;
            var marksToAdd = (int)t.Multiplier;
            var segment = t.Segment;

            if (!state.ContainsKey(playerId)) continue;

            var playerState = state[playerId];
            var currentMarks = playerState.Marks[segment];

            for (int m = 0; m < marksToAdd; m++)
            {
                if (currentMarks < 3)
                {
                    currentMarks++;
                    playerState.Marks[segment] = currentMarks;
                }
                else
                {
                    var opponents = state.Keys.Where(k => k != playerId);
                    var isClosedByAllOpponents = opponents.All(oId => state[oId].Marks[segment] >= 3);

                    if (!isClosedByAllOpponents)
                    {
                        playerState.Score += segment;
                    }
                }
            }
            state[playerId] = (playerState.Score, playerState.Marks);
        }

        return state;
    }
}
