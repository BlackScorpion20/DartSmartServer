using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

public class GamePlayer : Entity
{
    public Guid GameId { get; private set; }
    public Guid? UserId { get; private set; }  // Nullable for guest/bot players
    public PlayerType PlayerType { get; private set; }
    public string? DisplayName { get; private set; }  // Custom name for guests/bots
    public int PlayerOrder { get; private set; }
    public int? FinalScore { get; private set; }
    public int DartsThrown { get; private set; }
    public int PointsScored { get; private set; }
    public decimal PPD { get; private set; }
    public bool IsWinner { get; private set; }

    // Navigation properties
    public GameSession? Game { get; private set; }
    public User? User { get; private set; }

    private GamePlayer() : base()
    {
        DartsThrown = 0;
        PointsScored = 0;
        PPD = 0;
        IsWinner = false;
        PlayerType = PlayerType.Human;
    }

    public static GamePlayer Create(Guid gameId, Guid? userId, int playerOrder, PlayerType playerType = PlayerType.Human, string? displayName = null)
    {
        return new GamePlayer
        {
            GameId = gameId,
            UserId = userId == Guid.Empty ? null : userId,
            PlayerType = playerType,
            DisplayName = displayName,
            PlayerOrder = playerOrder,
            DartsThrown = 0,
            PointsScored = 0,
            PPD = 0,
            IsWinner = false
        };
    }

    /// <summary>
    /// Gets the display name for this player (username for humans, custom name for guests/bots)
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(DisplayName))
            return DisplayName;
        
        return User?.Username ?? PlayerType switch
        {
            PlayerType.Guest => $"Guest {PlayerOrder + 1}",
            PlayerType.Bot => $"Bot {PlayerOrder + 1}",
            _ => "Unknown"
        };
    }

    public void IncrementDartsThrown()
    {
        DartsThrown++;
        RecalculatePPD();
    }

    public void AddPoints(int points)
    {
        PointsScored += points;
        RecalculatePPD();
    }

    public void SetFinalScore(int score)
    {
        FinalScore = score;
    }

    public void MarkAsWinner()
    {
        IsWinner = true;
    }

    public void OverrideStats(int pointsScored, int dartsThrown)
    {
        PointsScored = pointsScored;
        DartsThrown = dartsThrown;
        RecalculatePPD();
    }

    private void RecalculatePPD()
    {
        PPD = DartsThrown > 0
            ? Math.Round((decimal)PointsScored / DartsThrown, 2)
            : 0;
    }
}

