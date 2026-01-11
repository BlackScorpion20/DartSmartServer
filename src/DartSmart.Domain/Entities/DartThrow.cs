using DartSmart.Domain.Common;

namespace DartSmart.Domain.Entities;

/// <summary>
/// DartThrow entity - represents a single dart throw
/// </summary>
public class DartThrow : Entity<DartThrowId>
{
    private const int MaxSegment = 25; // 25 = Bull
    private const int MinSegment = 0;  // 0 = Miss
    private const int MaxMultiplier = 3;
    private const int MaxDartNumber = 3;

    public GameId GameId { get; private init; } = null!;
    public PlayerId PlayerId { get; private init; } = null!;
    public int Segment { get; private init; }
    public int Multiplier { get; private init; }
    public int Points { get; private init; }
    public int Round { get; private init; }
    public int DartNumber { get; private init; }
    public DateTime Timestamp { get; private init; }
    public bool IsBust { get; private init; }

    private DartThrow() { } // EF Core constructor

    public static DartThrow Create(
        GameId gameId,
        PlayerId playerId,
        int segment,
        int multiplier,
        int round,
        int dartNumber,
        bool isBust = false)
    {
        ValidateSegment(segment);
        ValidateMultiplier(multiplier, segment);
        ValidateDartNumber(dartNumber);

        var points = CalculatePoints(segment, multiplier, isBust);

        return new DartThrow
        {
            Id = DartThrowId.New(),
            GameId = gameId ?? throw new ArgumentNullException(nameof(gameId)),
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId)),
            Segment = segment,
            Multiplier = multiplier,
            Points = points,
            Round = round,
            DartNumber = dartNumber,
            Timestamp = DateTime.UtcNow,
            IsBust = isBust
        };
    }

    private static void ValidateSegment(int segment)
    {
        if (segment < MinSegment || segment > MaxSegment)
            throw new ArgumentOutOfRangeException(nameof(segment), $"Segment must be between {MinSegment} and {MaxSegment}");
    }

    private static void ValidateMultiplier(int multiplier, int segment)
    {
        if (multiplier < 1 || multiplier > MaxMultiplier)
            throw new ArgumentOutOfRangeException(nameof(multiplier), $"Multiplier must be between 1 and {MaxMultiplier}");

        // Bull (25) can only be single (25) or double (50)
        if (segment == 25 && multiplier == 3)
            throw new ArgumentException("Bull cannot have triple multiplier", nameof(multiplier));
    }

    private static void ValidateDartNumber(int dartNumber)
    {
        if (dartNumber < 1 || dartNumber > MaxDartNumber)
            throw new ArgumentOutOfRangeException(nameof(dartNumber), $"Dart number must be between 1 and {MaxDartNumber}");
    }

    private static int CalculatePoints(int segment, int multiplier, bool isBust)
    {
        if (isBust || segment == 0) return 0;
        return segment * multiplier;
    }

    public bool IsDouble => Multiplier == 2;
    public bool IsTriple => Multiplier == 3;
    public bool IsBull => Segment == 25 && Multiplier == 1;
    public bool IsDoubleBull => Segment == 25 && Multiplier == 2;
    public bool Is180Contributor => Segment == 20 && Multiplier == 3; // T20
}
