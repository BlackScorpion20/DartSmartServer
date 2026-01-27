using DartSmartNet.Server.Domain.Common;

namespace DartSmartNet.Server.Domain.Entities;

public class PlayerStats : Entity
{
    // Basic Stats
    public Guid UserId { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public int GamesLost { get; private set; }
    public int TotalDartsThrown { get; private set; }
    public int TotalPointsScored { get; private set; }
    public decimal AveragePPD { get; private set; }
    public decimal AverageMPR { get; private set; }
    public int TotalCricketMarks { get; private set; }
    
    // High Scores
    public int HighestCheckout { get; private set; }
    public int HighestScore { get; private set; }
    public int Total180s { get; private set; }
    public int Total171s { get; private set; }
    public int Total140s { get; private set; }
    public int Total100Plus { get; private set; }
    
    // Checkout Statistics
    public int TotalCheckouts { get; private set; }
    public int TotalDoubleAttempts { get; private set; }
    public int TotalDoubleHits { get; private set; }
    
    // Session/Match Averages
    public decimal BestSessionAverage { get; private set; }
    public decimal WorstSessionAverage { get; private set; }
    public decimal TotalFirst9Points { get; private set; }
    public int TotalFirst9Attempts { get; private set; }
    
    // Streaks
    public int CurrentWinStreak { get; private set; }
    public int LongestWinStreak { get; private set; }
    public int CurrentLossStreak { get; private set; }
    public int LongestLossStreak { get; private set; }
    
    // Legs Statistics
    public int TotalLegsPlayed { get; private set; }
    public int TotalLegsWon { get; private set; }
    
    // Time-based
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastGameAt { get; private set; }

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
            AverageMPR = 0,
            TotalCricketMarks = 0,
            HighestCheckout = 0,
            HighestScore = 0,
            Total180s = 0,
            Total171s = 0,
            Total140s = 0,
            Total100Plus = 0,
            TotalCheckouts = 0,
            TotalDoubleAttempts = 0,
            TotalDoubleHits = 0,
            BestSessionAverage = 0,
            WorstSessionAverage = 0,
            TotalFirst9Points = 0,
            TotalFirst9Attempts = 0,
            CurrentWinStreak = 0,
            LongestWinStreak = 0,
            CurrentLossStreak = 0,
            LongestLossStreak = 0,
            TotalLegsPlayed = 0,
            TotalLegsWon = 0,
            UpdatedAt = DateTime.UtcNow,
            LastGameAt = null
        };
    }

    public void UpdateAfterGame(
        bool won, 
        int dartsThrown, 
        int pointsScored, 
        IEnumerable<int> roundScores, 
        int checkout,
        int doubleAttempts = 0,
        int doubleHits = 0,
        decimal sessionAverage = 0,
        decimal first9Average = 0,
        int legsPlayed = 1,
        int legsWon = 0,
        int cricketMarks = 0)
    {
        GamesPlayed++;
        LastGameAt = DateTime.UtcNow;
        
        // Win/Loss tracking
        if (won) 
        {
            GamesWon++;
            CurrentWinStreak++;
            CurrentLossStreak = 0;
            
            if (CurrentWinStreak > LongestWinStreak)
                LongestWinStreak = CurrentWinStreak;
        }
        else 
        {
            GamesLost++;
            CurrentLossStreak++;
            CurrentWinStreak = 0;
            
            if (CurrentLossStreak > LongestLossStreak)
                LongestLossStreak = CurrentLossStreak;
        }

        TotalDartsThrown += dartsThrown;
        TotalPointsScored += pointsScored;

        // Recalculate average PPD
        AveragePPD = TotalDartsThrown > 0
            ? Math.Round((decimal)TotalPointsScored / TotalDartsThrown, 2)
            : 0;

        // Calculate Average MPR (Marks per regular visit/round, simplified as marks / (darts/3))
        TotalCricketMarks += cricketMarks;
        var totalRounds = (decimal)TotalDartsThrown / 3;
        AverageMPR = totalRounds > 0
            ? Math.Round(TotalCricketMarks / totalRounds, 2)
            : 0;

        // Update high scores count
        foreach (var score in roundScores)
        {
            if (score == 180) Total180s++;
            else if (score == 171) Total171s++;
            else if (score >= 140) Total140s++;
            else if (score >= 100) Total100Plus++;
            
            if (score > HighestScore)
                HighestScore = score;
        }

        // Checkout stats
        if (checkout > 0)
        {
            TotalCheckouts++;
            if (checkout > HighestCheckout)
                HighestCheckout = checkout;
        }
        
        // Double tracking
        TotalDoubleAttempts += doubleAttempts;
        TotalDoubleHits += doubleHits;
        
        // Session average tracking
        if (sessionAverage > 0)
        {
            if (BestSessionAverage == 0 || sessionAverage > BestSessionAverage)
                BestSessionAverage = Math.Round(sessionAverage, 2);
            
            if (WorstSessionAverage == 0 || sessionAverage < WorstSessionAverage)
                WorstSessionAverage = Math.Round(sessionAverage, 2);
        }
        
        // First 9 average tracking
        if (first9Average > 0)
        {
            TotalFirst9Points += first9Average * 3; // Average * 3 visits = points
            TotalFirst9Attempts++;
        }
        
        // Legs tracking
        TotalLegsPlayed += legsPlayed;
        TotalLegsWon += legsWon;

        UpdatedAt = DateTime.UtcNow;
    }

    // Computed Properties
    public decimal WinRate => GamesPlayed > 0
        ? Math.Round((decimal)GamesWon / GamesPlayed * 100, 2)
        : 0;
    
    public decimal CheckoutPercentage => TotalDoubleAttempts > 0
        ? Math.Round((decimal)TotalDoubleHits / TotalDoubleAttempts * 100, 2)
        : 0;
    
    public decimal First9Average => TotalFirst9Attempts > 0
        ? Math.Round(TotalFirst9Points / TotalFirst9Attempts, 2)
        : 0;
    
    public decimal LegsWinRate => TotalLegsPlayed > 0
        ? Math.Round((decimal)TotalLegsWon / TotalLegsPlayed * 100, 2)
        : 0;
    
    public decimal ThreeDartAverage => TotalDartsThrown > 0
        ? Math.Round((decimal)TotalPointsScored / TotalDartsThrown * 3, 2)
        : 0;
}
