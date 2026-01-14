using DartSmartNet.Server.Domain.Common;

namespace DartSmartNet.Server.Domain.Entities;

public class PlayerStats : Entity
{
    public Guid UserId { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public int GamesLost { get; private set; }
    public int TotalDartsThrown { get; private set; }
    public int TotalPointsScored { get; private set; }
    public decimal AveragePPD { get; private set; }
    public int HighestCheckout { get; private set; }
    public int Total180s { get; private set; }
    public int Total171s { get; private set; }
    public int Total140s { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    private PlayerStats() : base()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public static PlayerStats CreateForUser(Guid userId)
    {
        return new PlayerStats
        {
            UserId = userId,
            GamesPlayed = 0,
            GamesWon = 0,
            GamesLost = 0,
            TotalDartsThrown = 0,
            TotalPointsScored = 0,
            AveragePPD = 0,
            HighestCheckout = 0,
            Total180s = 0,
            Total171s = 0,
            Total140s = 0,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateAfterGame(bool won, int dartsThrown, int pointsScored, IEnumerable<int> roundScores, int checkout)
    {
        GamesPlayed++;
        if (won) GamesWon++;
        else GamesLost++;

        TotalDartsThrown += dartsThrown;
        TotalPointsScored += pointsScored;

        // Recalculate average PPD
        AveragePPD = TotalDartsThrown > 0
            ? Math.Round((decimal)TotalPointsScored / TotalDartsThrown, 2)
            : 0;

        // Update high scores count
        foreach (var score in roundScores)
        {
            if (score == 180) Total180s++;
            else if (score == 171) Total171s++;
            else if (score >= 140) Total140s++;
        }

        if (checkout > HighestCheckout)
            HighestCheckout = checkout;

        UpdatedAt = DateTime.UtcNow;
    }

    public decimal WinRate => GamesPlayed > 0
        ? Math.Round((decimal)GamesWon / GamesPlayed * 100, 2)
        : 0;
}
