namespace DartSmartNet.Server.Domain.Entities;

/// <summary>
/// Represents a participant in a tournament
/// </summary>
public class TournamentParticipant
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Tournament? Tournament { get; private set; }
    
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    
    /// <summary>
    /// Seed position for bracket placement (1 = top seed)
    /// </summary>
    public int? Seed { get; private set; }
    
    /// <summary>
    /// Final placement in tournament (1 = winner)
    /// </summary>
    public int? FinalPlacement { get; private set; }
    
    /// <summary>
    /// Whether participant has been eliminated
    /// </summary>
    public bool IsEliminated { get; private set; }
    
    /// <summary>
    /// Number of matches won in this tournament
    /// </summary>
    public int MatchesWon { get; private set; }
    
    /// <summary>
    /// Number of matches lost in this tournament
    /// </summary>
    public int MatchesLost { get; private set; }
    
    /// <summary>
    /// Total legs won across all matches
    /// </summary>
    public int LegsWon { get; private set; }
    
    /// <summary>
    /// Total legs lost across all matches
    /// </summary>
    public int LegsLost { get; private set; }
    
    public DateTime JoinedAt { get; private set; }
    public DateTime? EliminatedAt { get; private set; }

    private TournamentParticipant() { }

    public static TournamentParticipant Create(Guid tournamentId, Guid userId, int? seed = null)
    {
        return new TournamentParticipant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            UserId = userId,
            Seed = seed,
            IsEliminated = false,
            MatchesWon = 0,
            MatchesLost = 0,
            LegsWon = 0,
            LegsLost = 0,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void SetSeed(int seed)
    {
        Seed = seed;
    }

    public void RecordMatchResult(bool won, int legsWon, int legsLost)
    {
        if (won)
        {
            MatchesWon++;
        }
        else
        {
            MatchesLost++;
        }
        
        LegsWon += legsWon;
        LegsLost += legsLost;
    }

    public void Eliminate(int placement)
    {
        IsEliminated = true;
        FinalPlacement = placement;
        EliminatedAt = DateTime.UtcNow;
    }

    public void SetAsWinner()
    {
        FinalPlacement = 1;
    }
}
