namespace DartSmartNet.Server.Domain.ValueObjects;

/// <summary>
/// Represents a player's skill rating for matchmaking purposes.
/// Uses a simplified ELO-like system.
/// </summary>
public readonly record struct SkillRating
{
    /// <summary>
    /// The numeric rating value (1000 = beginner, 1500 = intermediate, 2000+ = advanced)
    /// </summary>
    public int Rating { get; init; }
    
    /// <summary>
    /// Uncertainty factor (higher = less confident in the rating)
    /// </summary>
    public int Uncertainty { get; init; }
    
    /// <summary>
    /// Number of rated games played
    /// </summary>
    public int GamesPlayed { get; init; }

    public SkillRating(int rating = 1200, int uncertainty = 350, int gamesPlayed = 0)
    {
        Rating = Math.Clamp(rating, 0, 4000);
        Uncertainty = Math.Clamp(uncertainty, 50, 500);
        GamesPlayed = Math.Max(0, gamesPlayed);
    }

    /// <summary>
    /// Default rating for new players
    /// </summary>
    public static SkillRating Default => new(1200, 350, 0);

    /// <summary>
    /// Estimate skill rating from 3-dart average
    /// </summary>
    public static SkillRating FromThreeDartAverage(decimal average, int gamesPlayed = 0)
    {
        // Map average to rating:
        // 20-30 avg = 800-1000 (beginner)
        // 30-45 avg = 1000-1300 (casual)
        // 45-60 avg = 1300-1600 (intermediate)
        // 60-80 avg = 1600-2000 (advanced)
        // 80-100 avg = 2000-2500 (expert)
        // 100+ avg = 2500+ (professional)
        
        var rating = average switch
        {
            < 20 => 600,
            < 30 => 800 + (int)((average - 20) * 20),
            < 45 => 1000 + (int)((average - 30) * 20),
            < 60 => 1300 + (int)((average - 45) * 20),
            < 80 => 1600 + (int)((average - 60) * 20),
            < 100 => 2000 + (int)((average - 80) * 25),
            _ => 2500 + (int)((average - 100) * 10)
        };

        // Uncertainty decreases with more games
        var uncertainty = gamesPlayed switch
        {
            0 => 350,
            < 10 => 300,
            < 25 => 200,
            < 50 => 150,
            < 100 => 100,
            _ => 50
        };

        return new SkillRating(rating, uncertainty, gamesPlayed);
    }

    /// <summary>
    /// Calculate new ratings after a match
    /// </summary>
    public static (SkillRating winner, SkillRating loser) CalculateNewRatings(
        SkillRating winner, 
        SkillRating loser)
    {
        // K-factor based on uncertainty
        var kWinner = Math.Max(16, winner.Uncertainty / 5);
        var kLoser = Math.Max(16, loser.Uncertainty / 5);

        // Expected score
        var expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loser.Rating - winner.Rating) / 400.0));
        var expectedLoser = 1.0 - expectedWinner;

        // New ratings
        var newWinnerRating = winner.Rating + (int)(kWinner * (1.0 - expectedWinner));
        var newLoserRating = loser.Rating + (int)(kLoser * (0.0 - expectedLoser));

        // Reduce uncertainty
        var newWinnerUncertainty = Math.Max(50, winner.Uncertainty - 10);
        var newLoserUncertainty = Math.Max(50, loser.Uncertainty - 10);

        return (
            new SkillRating(newWinnerRating, newWinnerUncertainty, winner.GamesPlayed + 1),
            new SkillRating(newLoserRating, newLoserUncertainty, loser.GamesPlayed + 1)
        );
    }

    /// <summary>
    /// Check if two players are suitable for matching
    /// </summary>
    public bool IsGoodMatchWith(SkillRating other, int maxRatingDifference = 200)
    {
        // Consider uncertainty when matching
        var effectiveRange = maxRatingDifference + (Uncertainty + other.Uncertainty) / 2;
        return Math.Abs(Rating - other.Rating) <= effectiveRange;
    }

    /// <summary>
    /// Get skill tier name
    /// </summary>
    public string TierName => Rating switch
    {
        < 800 => "Beginner",
        < 1100 => "Casual",
        < 1400 => "Intermediate",
        < 1700 => "Advanced",
        < 2000 => "Expert",
        < 2400 => "Master",
        _ => "Legend"
    };

    public override string ToString() => $"{TierName} ({Rating}±{Uncertainty})";
}
