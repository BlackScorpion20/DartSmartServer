using DartSmartNet.Server.Domain.Common;

namespace DartSmartNet.Server.Domain.Entities;

public class GamePlayer : Entity
{
    public Guid GameId { get; private set; }
    public Guid UserId { get; private set; }
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
    }

    public static GamePlayer Create(Guid gameId, Guid userId, int playerOrder)
    {
        return new GamePlayer
        {
            GameId = gameId,
            UserId = userId,
            PlayerOrder = playerOrder,
            DartsThrown = 0,
            PointsScored = 0,
            PPD = 0,
            IsWinner = false
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
