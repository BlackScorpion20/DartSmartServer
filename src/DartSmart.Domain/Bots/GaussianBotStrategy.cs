using DartSmart.Domain.Entities;
using DartSmart.Domain.Common;
using DartSmart.Domain.ValueObjects;
using System.Security.Cryptography;

namespace DartSmart.Domain.Bots;

public class GaussianBotStrategy : IBotStrategy
{
    private readonly int _skillLevel; // 1-100
    private readonly double _standardDeviationMm;
    private readonly Random _random = new Random();

    public GaussianBotStrategy(int skillLevel)
    {
        _skillLevel = Math.Clamp(skillLevel, 1, 100);
        // Map 1-100 to Standard Deviation (in mm)
        // Level 100 = 5mm spread (Pro)
        // Level 50 = 40mm spread (Average)
        // Level 1 = 150mm spread (Beginner)
        _standardDeviationMm = MapSkillToSigma(_skillLevel);
    }

    private double MapSkillToSigma(int level)
    {
        // Simple linear interpolation for now, can be tuned
        // Y = mX + c
        // 1 -> 150
        // 100 -> 5
        // m = (5 - 150) / (100 - 1) = -145 / 99 ≈ -1.46
        return 150.0 + (level - 1) * ((5.0 - 150.0) / 99.0);
    }

    public BotThrowResult[] CalculateTurn(Game game, PlayerId botId)
    {
        var throws = new List<BotThrowResult>();
        // game.Players contains GamePlayer objects. GamePlayer has PlayerId property.
        if (!game.Players.Any(p => p.PlayerId == botId)) return Array.Empty<BotThrowResult>();

        // Current score logic needs to be accessed from Game entity or passed in.
        // Assuming we can get current score for the bot from the Game.
        // For X01, we need to know remaining score.
        // We'll throw 3 darts or until bust/win.
        
        // Simulation state (local score tracking for this turn)
        int currentScore = GetCurrentScore(game, botId); 

        for (int i = 1; i <= 3; i++)
        {
            if (currentScore <= 0 || (currentScore == 1 && game.OutMode == X01OutMode.DoubleOut)) break; // Bust or Win handled by Game logic ultimately, but we stop planning.

            var (targetSegment, targetMultiplier) = DetermineTarget(currentScore, game.OutMode);
            
            // Apply Physics
            var result = ThrowDart(targetSegment, targetMultiplier);

            throws.Add(new BotThrowResult(result.Segment, result.Multiplier, i));
            
            // Update local simulation score
            var points = result.Segment * result.Multiplier;
            
            // Basic bust check simply to stop throwing if we know we busted locally
            if (CheckBust(currentScore, points, game.OutMode))
            {
                break;
            }
            currentScore -= points;
            
            if (currentScore == 0) break; // Won
        }

        return throws.ToArray();
    }

    private int GetCurrentScore(Game game, PlayerId botId)
    {
        return game.GetPlayerScore(botId);
    }

    private (int Segment, int Multiplier) DetermineTarget(int currentScore, X01OutMode outMode)
    {
        // 1. Can we finish?
        if (CanFinish(currentScore, outMode))
        {
            return GetCheckoutTarget(currentScore);
        }

        // 2. Setup shot (if low score)
        
        // 3. Scoring (T20)
        return (20, 3);
    }

    private bool CanFinish(int score, X01OutMode outMode)
    {
        if (outMode == X01OutMode.DoubleOut)
             return score <= 170 && score != 169 && score != 168 && score != 166 && score != 165 && score != 163 && score != 162 && score != 159;
             // Simplified
        return false; 
    }

    private (int Segment, int Multiplier) GetCheckoutTarget(int score)
    {
        // Simple checkout logic (incomplete table)
        if (score == 50) return (25, 2);
        if (score == 40) return (20, 2);
        if (score <= 40 && score % 2 == 0) return (score / 2, 2);
        
        // Fallback for setup
        return (20, 1);
    }

    private (int Segment, int Multiplier) ThrowDart(int targetSegment, int targetMultiplier)
    {
        var targetCoords = DartboardGeometry.GetTargetCoordinates(targetSegment, targetMultiplier);

        // Box-Muller Transform for Gaussian Noise
        double u1 = 1.0 - _random.NextDouble(); 
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); 
        
        // Add component noise
        // We assume circular normal distribution (sigma_x = sigma_y)
        double noiseX = _skillLevel * randStdNormal * (_standardDeviationMm / 10.0); // Tuning factor
        
        // Resample for Y
        u1 = 1.0 - _random.NextDouble();
        u2 = 1.0 - _random.NextDouble();
        randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double noiseY = _skillLevel * randStdNormal * (_standardDeviationMm / 10.0);

        var actualX = targetCoords.X + noiseX;
        var actualY = targetCoords.Y + noiseY;

        return DartboardGeometry.GetScoreFromCoordinates(new ThrowCoordinates(actualX, actualY));
    }

    private bool CheckBust(int currentScore, int points, X01OutMode outMode)
    {
        int remaining = currentScore - points;
        if (remaining < 0) return true;
        if (remaining == 0) return false; // Check double out condition elsewhere, here assume hit was valid
        if (remaining == 1 && outMode == X01OutMode.DoubleOut) return true;
        return false;
    }
}
