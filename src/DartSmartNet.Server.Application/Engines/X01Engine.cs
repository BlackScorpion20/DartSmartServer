using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Engines;

public class X01Engine : IGameEngine
{
    private readonly IStatisticsService _statisticsService;

    public GameType GameType => GameType.X01;

    public X01Engine(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public int CalculateCurrentScore(GameSession game, Guid userId)
    {
        if (!game.StartingScore.HasValue)
            return 0;

        var remainingScore = game.StartingScore.Value;
        var hasOpened = game.Options.InMode != "Double";

        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.RoundNumber)
            .ThenBy(t => t.DartNumber)
            .GroupBy(t => t.RoundNumber);

        foreach (var round in playerThrows)
        {
            var roundTotal = 0;
            var roundBust = false;

            foreach (var dart in round)
            {
                if (!hasOpened)
                {
                    if (dart.Multiplier == Multiplier.Double)
                        hasOpened = true;
                    else
                        continue;
                }

                var newScore = remainingScore - roundTotal - dart.Points;

                if (newScore < 0 || (newScore == 1 && game.Options.OutMode != "Straight"))
                {
                    roundBust = true;
                    break;
                }

                if (newScore == 0)
                {
                    var validOut = (game.Options.OutMode == "Straight") ||
                                 (game.Options.OutMode == "Double" && dart.Multiplier == Multiplier.Double) ||
                                 (game.Options.OutMode == "Master" && (dart.Multiplier == Multiplier.Double || dart.Multiplier == Multiplier.Triple));

                    if (!validOut)
                    {
                        roundBust = true;
                        break;
                    }

                    roundTotal += dart.Points;
                    goto RoundFinished;
                }

                roundTotal += dart.Points;
            }

            if (!roundBust)
                remainingScore -= roundTotal;

            RoundFinished:;
            if (remainingScore == 0) break;
        }

        return remainingScore;
    }

    public bool CheckWinCondition(GameSession game, Guid userId, out int? finalScore)
    {
        var currentScore = CalculateCurrentScore(game, userId);
        finalScore = currentScore == 0 ? 0 : null;
        return currentScore == 0;
    }

    public async Task UpdateStatisticsAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        if (!game.StartingScore.HasValue) return;

        foreach (var player in game.Players)
        {
            if (!player.UserId.HasValue) continue;  // Skip bot players

            var validPoints = 0;
            var totalDarts = 0;
            var currentTotal = game.StartingScore.Value;
            var hasOpened = game.Options.InMode != "Double";
            var roundScoresList = new List<int>();

            var roundGroups = game.Throws
                .Where(t => t.UserId == player.UserId.Value)
                .OrderBy(t => t.RoundNumber)
                .ThenBy(t => t.DartNumber)
                .GroupBy(t => t.RoundNumber);

            foreach (var round in roundGroups)
            {
                var roundPoints = 0;
                var roundDarts = 0;
                var roundBust = false;

                foreach (var dart in round)
                {
                    roundDarts++;
                    if (!hasOpened)
                    {
                        if (dart.Multiplier == Multiplier.Double)
                            hasOpened = true;
                        else
                            continue;
                    }

                    var tempScore = currentTotal - roundPoints - dart.Points;

                    if (tempScore < 0 || (tempScore == 1 && game.Options.OutMode != "Straight"))
                    {
                        roundBust = true;
                        break;
                    }

                    if (tempScore == 0)
                    {
                        var validOut = (game.Options.OutMode == "Straight") ||
                                     (game.Options.OutMode == "Double" && dart.Multiplier == Multiplier.Double) ||
                                     (game.Options.OutMode == "Master" && (dart.Multiplier == Multiplier.Double || dart.Multiplier == Multiplier.Triple));

                        if (!validOut)
                        {
                            roundBust = true;
                            break;
                        }

                        roundPoints += dart.Points;
                        currentTotal = 0;
                        goto ProcessingComplete;
                    }

                    roundPoints += dart.Points;
                }

                if (!roundBust)
                {
                    validPoints += roundPoints;
                    currentTotal -= roundPoints;
                    roundScoresList.Add(roundPoints);
                }
                else
                {
                    roundScoresList.Add(0);
                }
                totalDarts += roundDarts;
            }

            ProcessingComplete:
            if (currentTotal == 0)
                validPoints = game.StartingScore.Value;

            player.OverrideStats(validPoints, totalDarts);

            var lastThrow = game.Throws
                .Where(t => t.UserId == player.UserId.Value)
                .OrderByDescending(t => t.RoundNumber)
                .ThenByDescending(t => t.DartNumber)
                .FirstOrDefault();

            var checkout = player.IsWinner && currentTotal == 0 && lastThrow != null
                ? lastThrow.Points
                : 0;

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId.Value,
                player.IsWinner,
                totalDarts,
                validPoints,
                roundScoresList,
                checkout,
                0,
                cancellationToken);
        }
    }

    public Dictionary<int, int>? GetPlayerState(GameSession game, Guid userId) => null;
}
