using DartSmart.Domain.Common;

namespace DartSmart.Domain.Entities;

/// <summary>
/// GamePlayer join entity - tracks player's state within a game
/// </summary>
public class GamePlayer : Entity<GamePlayer.GamePlayerId>
{
    public sealed record GamePlayerId(GameId GameId, PlayerId PlayerId);

    public PlayerId PlayerId { get; private init; } = null!;
    public GameId GameId { get; private init; } = null!;
    public int CurrentScore { get; private set; }
    public int TurnOrder { get; private init; }
    public int DartsThrown { get; private set; }
    public int LegsWon { get; private set; }
    public int SetsWon { get; private set; }
    public DateTime JoinedAt { get; private init; }

    private GamePlayer() { } // EF Core constructor

    public static GamePlayer Create(GameId gameId, PlayerId playerId, int startScore, int turnOrder)
    {
        return new GamePlayer
        {
            Id = new GamePlayerId(gameId, playerId),
            GameId = gameId ?? throw new ArgumentNullException(nameof(gameId)),
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId)),
            CurrentScore = startScore,
            TurnOrder = turnOrder,
            DartsThrown = 0,
            LegsWon = 0,
            SetsWon = 0,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void SubtractScore(int points)
    {
        if (points < 0)
            throw new ArgumentException("Points cannot be negative", nameof(points));
        
        CurrentScore -= points;
    }

    public void ResetScore(int startScore)
    {
        CurrentScore = startScore;
    }

    public void IncrementDartsThrown(int count = 1)
    {
        DartsThrown += count;
    }

    public void WinLeg()
    {
        LegsWon++;
    }

    public void WinSet()
    {
        SetsWon++;
    }

    public void BustTurn(int scoreBeforeTurn)
    {
        CurrentScore = scoreBeforeTurn;
    }
}
