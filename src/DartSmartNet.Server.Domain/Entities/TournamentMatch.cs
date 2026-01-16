using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

/// <summary>
/// Represents a single match within a tournament bracket
/// </summary>
public class TournamentMatch
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Tournament? Tournament { get; private set; }
    
    /// <summary>
    /// Round number in the bracket (1 = first round, etc.)
    /// </summary>
    public int Round { get; private set; }
    
    /// <summary>
    /// Position within the round (for bracket placement)
    /// </summary>
    public int MatchNumber { get; private set; }
    
    /// <summary>
    /// For double elimination: true if this is in the losers bracket
    /// </summary>
    public bool IsLosersBracket { get; private set; }
    
    /// <summary>
    /// Player 1 participant reference
    /// </summary>
    public Guid? Player1Id { get; private set; }
    public TournamentParticipant? Player1 { get; private set; }
    
    /// <summary>
    /// Player 2 participant reference
    /// </summary>
    public Guid? Player2Id { get; private set; }
    public TournamentParticipant? Player2 { get; private set; }
    
    /// <summary>
    /// Winner of this match
    /// </summary>
    public Guid? WinnerId { get; private set; }
    public TournamentParticipant? Winner { get; private set; }
    
    /// <summary>
    /// Reference to the actual game session for this match
    /// </summary>
    public Guid? GameSessionId { get; private set; }
    public GameSession? GameSession { get; private set; }
    
    /// <summary>
    /// Match ID that the winner advances to
    /// </summary>
    public Guid? NextMatchId { get; private set; }
    public TournamentMatch? NextMatch { get; private set; }
    
    /// <summary>
    /// For double elimination: match ID that the loser goes to
    /// </summary>
    public Guid? LoserNextMatchId { get; private set; }
    public TournamentMatch? LoserNextMatch { get; private set; }
    
    public TournamentMatchStatus Status { get; private set; }
    
    /// <summary>
    /// Legs won by Player 1
    /// </summary>
    public int Player1Legs { get; private set; }
    
    /// <summary>
    /// Legs won by Player 2
    /// </summary>
    public int Player2Legs { get; private set; }
    
    /// <summary>
    /// Sets won by Player 1 (if playing sets)
    /// </summary>
    public int Player1Sets { get; private set; }
    
    /// <summary>
    /// Sets won by Player 2 (if playing sets)
    /// </summary>
    public int Player2Sets { get; private set; }
    
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TournamentMatch() { }

    public static TournamentMatch Create(
        Guid tournamentId,
        int round,
        int matchNumber,
        bool isLosersBracket = false,
        Guid? player1Id = null,
        Guid? player2Id = null,
        Guid? nextMatchId = null,
        Guid? loserNextMatchId = null)
    {
        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Round = round,
            MatchNumber = matchNumber,
            IsLosersBracket = isLosersBracket,
            Player1Id = player1Id,
            Player2Id = player2Id,
            NextMatchId = nextMatchId,
            LoserNextMatchId = loserNextMatchId,
            Status = TournamentMatchStatus.Pending,
            Player1Legs = 0,
            Player2Legs = 0,
            Player1Sets = 0,
            Player2Sets = 0
        };
        
        // If both players are assigned, match is ready
        if (player1Id.HasValue && player2Id.HasValue)
        {
            match.Status = TournamentMatchStatus.Ready;
        }
        
        return match;
    }

    public void AssignPlayer1(Guid participantId)
    {
        Player1Id = participantId;
        CheckIfReady();
    }

    public void AssignPlayer2(Guid participantId)
    {
        Player2Id = participantId;
        CheckIfReady();
    }

    private void CheckIfReady()
    {
        if (Player1Id.HasValue && Player2Id.HasValue && Status == TournamentMatchStatus.Pending)
        {
            Status = TournamentMatchStatus.Ready;
        }
    }

    public void Start(Guid gameSessionId)
    {
        if (Status != TournamentMatchStatus.Ready)
            throw new InvalidOperationException("Match is not ready to start");
        
        GameSessionId = gameSessionId;
        Status = TournamentMatchStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void UpdateScore(int player1Legs, int player2Legs, int player1Sets = 0, int player2Sets = 0)
    {
        Player1Legs = player1Legs;
        Player2Legs = player2Legs;
        Player1Sets = player1Sets;
        Player2Sets = player2Sets;
    }

    public void Complete(Guid winnerId)
    {
        if (Status != TournamentMatchStatus.InProgress)
            throw new InvalidOperationException("Match must be in progress to complete");
        
        if (winnerId != Player1Id && winnerId != Player2Id)
            throw new ArgumentException("Winner must be one of the players", nameof(winnerId));
        
        WinnerId = winnerId;
        Status = TournamentMatchStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Walkover(Guid winnerId)
    {
        if (winnerId != Player1Id && winnerId != Player2Id)
            throw new ArgumentException("Winner must be one of the players", nameof(winnerId));
        
        WinnerId = winnerId;
        Status = TournamentMatchStatus.Walkover;
        CompletedAt = DateTime.UtcNow;
    }

    public Guid? GetLoserId()
    {
        if (!WinnerId.HasValue) return null;
        return WinnerId == Player1Id ? Player2Id : Player1Id;
    }
}
