namespace DartSmartNet.Server.Domain.ValueObjects;

public sealed record Score
{
    public int Segment { get; init; }
    public Multiplier Multiplier { get; init; }
    public int Points => CalculatePoints();

    private Score(int segment, Multiplier multiplier)
    {
        Segment = segment;
        Multiplier = multiplier;
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
    public static Score Miss() => new(0, Multiplier.Single);
    public static Score Single(int segment) => new(segment, Multiplier.Single);
    public static Score Double(int segment) => new(segment, Multiplier.Double);
    public static Score Triple(int segment) => new(segment, Multiplier.Triple);
    public static Score SingleBull() => new(25, Multiplier.Single);
    public static Score DoubleBull() => new(25, Multiplier.Double);

    public override string ToString()
    {
        if (Segment == 0) return "MISS";
        if (Segment == 25 && Multiplier == Multiplier.Double) return "BULL";
        if (Segment == 25) return "25";

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
