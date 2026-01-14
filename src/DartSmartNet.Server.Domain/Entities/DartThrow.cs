using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Domain.Entities;

public class DartThrow : Entity
{
    public Guid GameId { get; private set; }
    public Guid UserId { get; private set; }
    public int RoundNumber { get; private set; }
    public int DartNumber { get; private set; }
    public int Segment { get; private set; }
    public Multiplier Multiplier { get; private set; }
    public int Points { get; private set; }
    public DateTime ThrownAt { get; private set; }
    public byte[]? RawData { get; private set; }

    // Navigation properties
    public GameSession? Game { get; private set; }
    public User? User { get; private set; }

    private DartThrow() : base()
    {
        ThrownAt = DateTime.UtcNow;
    }

    public static DartThrow Create(Guid gameId, Guid userId, int roundNumber, int dartNumber, Score score, byte[]? rawData = null)
    {
        return new DartThrow
        {
            GameId = gameId,
            UserId = userId,
            RoundNumber = roundNumber,
            DartNumber = dartNumber,
            Segment = score.Segment,
            Multiplier = score.Multiplier,
            Points = score.Points,
            ThrownAt = DateTime.UtcNow,
            RawData = rawData
        };
    }

    public Score GetScore()
    {
        return Multiplier switch
        {
            Multiplier.Single when Segment == 0 => Score.Miss(),
            Multiplier.Single when Segment == 25 => Score.SingleBull(),
            Multiplier.Double when Segment == 25 => Score.DoubleBull(),
            Multiplier.Single => Score.Single(Segment),
            Multiplier.Double => Score.Double(Segment),
            Multiplier.Triple => Score.Triple(Segment),
            _ => Score.Miss()
        };
    }
}
