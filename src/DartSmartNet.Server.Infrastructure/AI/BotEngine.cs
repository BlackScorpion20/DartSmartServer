using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Infrastructure.AI;

public class BotEngine : IBotService
{
    private readonly Random _random = new();
    private const int MaxSegment = 20;
    private const int BullseyeSegment = 25;

    public Task<Score> SimulateThrowAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default)
    {
        var score = SimulateThrow(difficulty, currentScore);
        return Task.FromResult(score);
    }

    public Task<IEnumerable<Score>> SimulateRoundAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default)
    {
        var scores = new List<Score>();
        var remainingScore = currentScore;

        for (int i = 0; i < 3; i++)
        {
            var score = SimulateThrow(difficulty, remainingScore);
            scores.Add(score);

            remainingScore -= score.Points;

            // If bust or finished, stop throwing
            if (remainingScore < 0 || remainingScore == 0)
            {
                break;
            }
        }

        return Task.FromResult<IEnumerable<Score>>(scores);
    }

    private Score SimulateThrow(BotDifficulty difficulty, int currentScore)
    {
        var (avgPpd, consistency, checkoutSkill) = GetBotParameters(difficulty);

        // Determine if this is a checkout situation (score <= 170)
        bool isCheckoutAttempt = currentScore <= 170 && currentScore > 1;

        if (isCheckoutAttempt)
        {
            return SimulateCheckoutAttempt(currentScore, checkoutSkill, consistency);
        }

        // Normal scoring throw - aim for high value targets
        return SimulateNormalThrow(avgPpd, consistency);
    }

    private Score SimulateNormalThrow(decimal avgPpd, decimal consistency)
    {
        // Determine target based on strategy
        // Professional players typically aim for T20, T19, T18
        var targetSegment = DetermineTargetSegment(consistency);
        var targetMultiplier = DetermineTargetMultiplier(avgPpd);

        // Apply accuracy/variance
        return ApplyAccuracy(targetSegment, targetMultiplier, consistency);
    }

    private Score SimulateCheckoutAttempt(int remainingScore, decimal checkoutSkill, decimal consistency)
    {
        // Calculate ideal checkout target
        var checkoutTarget = CalculateCheckoutTarget(remainingScore);

        if (checkoutTarget == null)
        {
            // No valid checkout or too complex, aim for setup or scoring
            return SimulateNormalThrow(60m, consistency); // Aim high to set up checkout
        }

        // Attempt the checkout with skill-based success probability
        var successChance = _random.NextDouble();

        if (successChance < (double)checkoutSkill)
        {
            // Successful checkout
            return checkoutTarget;
        }

        // Miss the checkout - hit a related segment or miss
        return ApplyAccuracy(checkoutTarget.Segment, checkoutTarget.Multiplier, consistency * 0.7m);
    }

    private Score? CalculateCheckoutTarget(int score)
    {
        // Common checkout doubles
        if (score <= 40 && score % 2 == 0)
        {
            return Score.Double(score / 2);
        }

        // Bullseye checkout
        if (score == 50)
        {
            return Score.DoubleBull();
        }

        // High checkouts (T20 + Double)
        if (score >= 61 && score <= 100)
        {
            // Try T20 + Double combination
            var afterTriple = score - 60;
            if (afterTriple <= 40 && afterTriple % 2 == 0)
            {
                return Score.Triple(20); // Hit T20, next dart will aim for double
            }
        }

        // High checkouts (T19 + Double)
        if (score >= 57 && score <= 97)
        {
            var afterTriple = score - 57;
            if (afterTriple <= 40 && afterTriple % 2 == 0)
            {
                return Score.Triple(19);
            }
        }

        // No simple checkout available
        return null;
    }

    private int DetermineTargetSegment(decimal consistency)
    {
        // Professional players aim for T20, T19, T18 area
        var random = _random.NextDouble();

        if (random < (double)consistency)
        {
            // Aim for optimal targets
            return _random.Next(0, 100) switch
            {
                < 60 => 20,  // 60% T20
                < 85 => 19,  // 25% T19
                < 95 => 18,  // 10% T18
                _ => 17      // 5% T17
            };
        }

        // Less consistent - might aim for other high segments
        return _random.Next(15, 21);
    }

    private Multiplier DetermineTargetMultiplier(decimal avgPpd)
    {
        // Higher PPD means more triple attempts
        var tripleAttemptChance = Math.Min((double)avgPpd / 100.0, 0.85);
        var doubleAttemptChance = 0.10;

        var random = _random.NextDouble();

        if (random < tripleAttemptChance)
        {
            return Multiplier.Triple;
        }

        if (random < tripleAttemptChance + doubleAttemptChance)
        {
            return Multiplier.Double;
        }

        return Multiplier.Single;
    }

    private Score ApplyAccuracy(int targetSegment, Multiplier targetMultiplier, decimal consistency)
    {
        // Higher consistency = more likely to hit target
        var hitTarget = _random.NextDouble() < (double)consistency;

        if (hitTarget)
        {
            // Hit the intended target
            return CreateScore(targetSegment, targetMultiplier);
        }

        // Miss - apply variance
        return ApplyMiss(targetSegment, targetMultiplier, consistency);
    }

    private Score ApplyMiss(int targetSegment, Multiplier targetMultiplier, decimal consistency)
    {
        var missType = _random.NextDouble();

        // Complete miss (off the board)
        if (missType < 0.05 * (1 - (double)consistency))
        {
            return Score.Miss();
        }

        // Hit wrong multiplier (very common miss)
        if (missType < 0.6)
        {
            // If aiming for triple, might hit single or double of same number
            if (targetMultiplier == Multiplier.Triple)
            {
                var multiplier = _random.NextDouble() < 0.7 ? Multiplier.Single : Multiplier.Double;
                return CreateScore(targetSegment, multiplier);
            }

            // If aiming for double, might hit single
            if (targetMultiplier == Multiplier.Double)
            {
                return CreateScore(targetSegment, Multiplier.Single);
            }

            // Already aiming single, hit it
            return CreateScore(targetSegment, Multiplier.Single);
        }

        // Hit adjacent segment
        var adjacentSegment = GetAdjacentSegment(targetSegment);

        // Might hit adjacent with different multiplier
        var newMultiplier = _random.NextDouble() < 0.5 ? targetMultiplier : Multiplier.Single;

        return CreateScore(adjacentSegment, newMultiplier);
    }

    private int GetAdjacentSegment(int segment)
    {
        // Dartboard adjacent segments (clockwise/counter-clockwise)
        var adjacents = segment switch
        {
            20 => new[] { 1, 5 },
            1 => new[] { 20, 18 },
            18 => new[] { 1, 4 },
            4 => new[] { 18, 13 },
            13 => new[] { 4, 6 },
            6 => new[] { 13, 10 },
            10 => new[] { 6, 15 },
            15 => new[] { 10, 2 },
            2 => new[] { 15, 17 },
            17 => new[] { 2, 3 },
            3 => new[] { 17, 19 },
            19 => new[] { 3, 7 },
            7 => new[] { 19, 16 },
            16 => new[] { 7, 8 },
            8 => new[] { 16, 11 },
            11 => new[] { 8, 14 },
            14 => new[] { 11, 9 },
            9 => new[] { 14, 12 },
            12 => new[] { 9, 5 },
            5 => new[] { 12, 20 },
            25 => new[] { 25 }, // Bullseye has no real adjacents, stay in bull area
            _ => new[] { 1, 5 }
        };

        return adjacents[_random.Next(adjacents.Length)];
    }

    private Score CreateScore(int segment, Multiplier multiplier)
    {
        return multiplier switch
        {
            Multiplier.Single => Score.Single(segment),
            Multiplier.Double when segment == 25 => Score.DoubleBull(),
            Multiplier.Double => Score.Double(segment),
            Multiplier.Triple => Score.Triple(segment),
            _ => Score.Single(segment)
        };
    }

    private (decimal avgPpd, decimal consistency, decimal checkoutSkill) GetBotParameters(BotDifficulty difficulty)
    {
        return difficulty switch
        {
            BotDifficulty.Easy => (35m, 0.3m, 0.2m),
            BotDifficulty.Medium => (55m, 0.5m, 0.4m),
            BotDifficulty.Hard => (75m, 0.7m, 0.6m),
            BotDifficulty.Expert => (90m, 0.85m, 0.8m),
            _ => (35m, 0.3m, 0.2m)
        };
    }
}
