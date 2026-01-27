using System.Text.Json.Serialization;

namespace DartSmartNet.Server.Domain.ValueObjects;

public sealed record Score
{
    public int Segment { get; init; }
    public Multiplier Multiplier { get; init; }
    public bool IsOuter { get; init; }
    public int Points => CalculatePoints();

    [JsonConstructor]
    public Score(int segment, Multiplier multiplier, bool isOuter = false)
    {
        Segment = segment;
        Multiplier = multiplier;
        IsOuter = isOuter;
    }

    private int CalculatePoints()
    {
        if (Segment == 0) return 0; // Miss
        if (Segment == 25) return Multiplier == Multiplier.Double ? 50 : 25; // Bullseye
        return Segment * (int)Multiplier;
    }

    public bool IsDouble() => Multiplier == Multiplier.Double;
    public bool IsTriple() => Multiplier == Multiplier.Triple;
    public bool IsBullseye() => Segment == 25;

    // Factory methods
    public static Score Miss() => new(0, Multiplier.Single, false);
    public static Score Single(int segment, bool isOuter = false) => new(segment, Multiplier.Single, isOuter);
    public static Score Double(int segment) => new(segment, Multiplier.Double, true);
    public static Score Triple(int segment) => new(segment, Multiplier.Triple, false);
    public static Score SingleBull() => new(25, Multiplier.Single, false);
    public static Score DoubleBull() => new(25, Multiplier.Double, false);

    public override string ToString()
    {
        if (Segment == 0) return "MISS";
        if (Segment == 25)
        {
            // SingleBull (25x1) -> "25", DoubleBull (25x2) -> "BULL"
            return Multiplier == Multiplier.Double ? "BULL" : "25";
        }

        return Multiplier switch
        {
            Multiplier.Single => $"{Segment}",
            Multiplier.Double => $"D{Segment}",
            Multiplier.Triple => $"T{Segment}",
            _ => $"{Segment}"
        };
    }
}

public enum Multiplier
{
    Single = 1,
    Double = 2,
    Triple = 3
}
