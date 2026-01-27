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

public class AroundTheClockEngine : IGameEngine
{
    private readonly IStatisticsService _statisticsService;
    private static readonly int[] Targets = Enumerable.Range(1, 20).Append(25).ToArray();

    public GameType GameType => GameType.AroundTheClock;

    public AroundTheClockEngine(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public int CalculateCurrentScore(GameSession game, Guid userId)
    {
        var targetIndex = CalculateProgress(game, userId);
        return targetIndex < Targets.Length ? Targets[targetIndex] : 0;
    }

    public bool CheckWinCondition(GameSession game, Guid userId, out int? finalScore)
    {
        var progress = CalculateProgress(game, userId);
        if (progress >= Targets.Length)
        {
            finalScore = game.Throws.Count(t => t.UserId == userId);
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

            var totalDarts = game.Throws.Count(t => t.UserId == player.UserId.Value);
            var successfulHits = CalculateProgress(game, player.UserId.Value);
            
            // For ATC, "Points" could be the number of targets hit or darts taken.
            // Let's use targets hit as score for leaderboard/stats consistency if needed, 
            // but usually ATC is about finishing.
            player.OverrideStats(successfulHits, totalDarts);

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId.Value,
                player.IsWinner,
                totalDarts,
                successfulHits,
                new List<int>(), // No round scores for ATC in generic stats
                0,
                0,
                cancellationToken);
        }
    }

    public Dictionary<int, int>? GetPlayerState(GameSession game, Guid userId)
    {
        var progress = CalculateProgress(game, userId);
        var target = progress < Targets.Length ? Targets[progress] : 25; // Default to Bull if finished or last
        
        return new Dictionary<int, int>
        {
            { 0, progress }, // Index 0: Progress (how many targets hit)
            { 1, target }    // Index 1: Current target segment
        };
    }

    private int CalculateProgress(GameSession game, Guid userId)
    {
        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ThrownAt);

        int progress = 0;
        foreach (var t in playerThrows)
        {
            if (progress >= Targets.Length) break;

            if (t.Segment == Targets[progress])
            {
                progress++;
            }
        }
        return progress;
    }
}
