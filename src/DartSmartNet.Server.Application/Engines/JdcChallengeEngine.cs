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

public class JdcChallengeEngine : IGameEngine
{
    private readonly IStatisticsService _statisticsService;

    public GameType GameType => GameType.JdcChallenge;

    public JdcChallengeEngine(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public int CalculateCurrentScore(GameSession game, Guid userId)
    {
        var state = CalculateJdcState(game, userId);
        return state.TotalScore;
    }

    public bool CheckWinCondition(GameSession game, Guid userId, out int? finalScore)
    {
        var playerThrows = game.Throws.Where(t => t.UserId == userId).Count();
        if (playerThrows >= 99)
        {
            var state = CalculateJdcState(game, userId);
            finalScore = state.TotalScore;
            return true;
        }
        finalScore = null;
        return false;
    }

    public async Task UpdateStatisticsAsync(GameSession game, CancellationToken cancellationToken = default)
    {
        foreach (var player in game.Players)
        {
            if (!player.UserId.HasValue) continue;

            var state = CalculateJdcState(game, player.UserId.Value);
            var totalDarts = game.Throws.Count(t => t.UserId == player.UserId.Value);

            player.OverrideStats(state.TotalScore, totalDarts);

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId.Value,
                player.IsWinner,
                totalDarts,
                state.TotalScore,
                new List<int>(), 
                0,
                0,
                cancellationToken);
        }
    }

    public Dictionary<int, int>? GetPlayerState(GameSession game, Guid userId)
    {
        var state = CalculateJdcState(game, userId);
        return new Dictionary<int, int>
        {
            { 0, state.Part },
            { 1, state.CurrentTarget },
            { 2, state.TotalScore },
            { 3, state.DartsThrownAtCurrentTarget }
        };
    }

    private JdcState CalculateJdcState(GameSession game, Guid userId)
    {
        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ThrownAt)
            .ToList();

        int totalScore = 0;
        int throwIdx = 0;

        // Part 1: Targets 10-15
        int[] part1Targets = { 10, 11, 12, 13, 14, 15 };
        foreach (var target in part1Targets)
        {
            if (throwIdx + 3 > playerThrows.Count) return new JdcState(1, target, totalScore, playerThrows.Count - throwIdx);
            
            var visit = playerThrows.GetRange(throwIdx, 3);
            totalScore += CalculateVisitScore(visit, target, true);
            throwIdx += 3;
        }

        // Part 2: Doubles 1-20 + Bull
        int[] part2Targets = Enumerable.Range(1, 20).Append(25).ToArray();
        foreach (var target in part2Targets)
        {
            if (throwIdx + 3 > playerThrows.Count) return new JdcState(2, target, totalScore, playerThrows.Count - throwIdx);

            var visit = playerThrows.GetRange(throwIdx, 3);
            totalScore += CalculateVisitScore(visit, target, false, true);
            throwIdx += 3;
        }

        // Part 3: Targets 15-20
        int[] part3Targets = { 15, 16, 17, 18, 19, 20 };
        foreach (var target in part3Targets)
        {
            if (throwIdx + 3 > playerThrows.Count) return new JdcState(3, target, totalScore, playerThrows.Count - throwIdx);

            var visit = playerThrows.GetRange(throwIdx, 3);
            totalScore += CalculateVisitScore(visit, target, true);
            throwIdx += 3;
        }

        return new JdcState(4, 0, totalScore, 0); // Finished
    }

    private int CalculateVisitScore(List<DartThrow> visit, int target, bool allowShanghai, bool doublesOnly = false)
    {
        int score = 0;
        bool hitSingle = false;
        bool hitDouble = false;
        bool hitTriple = false;

        foreach (var t in visit)
        {
            if (t.Segment == target)
            {
                if (doublesOnly)
                {
                    if (t.Multiplier == Multiplier.Double) score += target * 2;
                }
                else
                {
                    score += t.Points;
                    if (t.Multiplier == Multiplier.Single) hitSingle = true;
                    if (t.Multiplier == Multiplier.Double) hitDouble = true;
                    if (t.Multiplier == Multiplier.Triple) hitTriple = true;
                }
            }
        }

        if (allowShanghai && hitSingle && hitDouble && hitTriple)
        {
            score += 100;
        }

        return score;
    }

    private record JdcState(int Part, int CurrentTarget, int TotalScore, int DartsThrownAtCurrentTarget);
}
