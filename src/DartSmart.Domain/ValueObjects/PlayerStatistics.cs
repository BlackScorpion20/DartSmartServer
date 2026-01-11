namespace DartSmart.Domain.ValueObjects;

/// <summary>
/// Value object for player statistics
/// </summary>
public sealed record PlayerStatistics
{
    public int TotalGames { get; init; }
    public int Wins { get; init; }
    public int Best3DartScore { get; init; }
    public int Count180s { get; init; }
    public int HighestCheckout { get; init; }
    public int TotalDarts { get; init; }
    public int TotalPoints { get; init; }

    public decimal WinRate => TotalGames > 0 ? (decimal)Wins / TotalGames * 100 : 0;
    public decimal AveragePerDart => TotalDarts > 0 ? (decimal)TotalPoints / TotalDarts : 0;
    public decimal Average3Dart => TotalDarts > 0 ? (decimal)TotalPoints / TotalDarts * 3 : 0;

    public static PlayerStatistics Empty => new()
    {
        TotalGames = 0,
        Wins = 0,
        Best3DartScore = 0,
        Count180s = 0,
        HighestCheckout = 0,
        TotalDarts = 0,
        TotalPoints = 0
    };

    public PlayerStatistics WithGame(bool isWin, int dartsThrown, int pointsScored, int? checkoutScore = null)
    {
        var best3Dart = pointsScored > 0 && dartsThrown >= 3 
            ? Math.Max(Best3DartScore, (int)Math.Round((decimal)pointsScored / dartsThrown * 3))
            : Best3DartScore;

        return this with
        {
            TotalGames = TotalGames + 1,
            Wins = isWin ? Wins + 1 : Wins,
            TotalDarts = TotalDarts + dartsThrown,
            TotalPoints = TotalPoints + pointsScored,
            Best3DartScore = best3Dart,
            HighestCheckout = checkoutScore.HasValue ? Math.Max(HighestCheckout, checkoutScore.Value) : HighestCheckout
        };
    }

    public PlayerStatistics With180() => this with { Count180s = Count180s + 1 };
}
