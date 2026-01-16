using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

/// <summary>
/// Represents a dart tournament with bracket structure
/// </summary>
public class Tournament
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    
    /// <summary>
    /// User who created/organizes this tournament
    /// </summary>
    public Guid OrganizerId { get; private set; }
    public User? Organizer { get; private set; }
    
    public TournamentFormat Format { get; private set; }
    public TournamentStatus Status { get; private set; }
    
    /// <summary>
    /// Game settings for all matches in this tournament
    /// </summary>
    public GameType GameType { get; private set; }
    public int StartingScore { get; private set; }
    public int LegsToWin { get; private set; } = 3;
    public int SetsToWin { get; private set; } = 1;
    
    /// <summary>
    /// Maximum number of participants allowed
    /// </summary>
    public int MaxParticipants { get; private set; }
    
    /// <summary>
    /// Minimum participants required to start
    /// </summary>
    public int MinParticipants { get; private set; } = 2;
    
    /// <summary>
    /// Whether the tournament is public or invite-only
    /// </summary>
    public bool IsPublic { get; private set; }
    
    /// <summary>
    /// Optional password for private tournaments
    /// </summary>
    public string? JoinCode { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? ScheduledStartAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Winner of the tournament (set when completed)
    /// </summary>
    public Guid? WinnerId { get; private set; }
    public User? Winner { get; private set; }
    
    /// <summary>
    /// Tournament participants
    /// </summary>
    public ICollection<TournamentParticipant> Participants { get; private set; } = new List<TournamentParticipant>();
    
    /// <summary>
    /// Tournament matches/bracket
    /// </summary>
    public ICollection<TournamentMatch> Matches { get; private set; } = new List<TournamentMatch>();

    private Tournament() { }

    public static Tournament Create(
        Guid organizerId,
        string name,
        TournamentFormat format,
        GameType gameType,
        int startingScore,
        int maxParticipants,
        bool isPublic = true,
        string? description = null,
        int legsToWin = 3,
        int setsToWin = 1,
        DateTime? scheduledStartAt = null)
    {
        if (maxParticipants < 2)
            throw new ArgumentException("Tournament must allow at least 2 participants", nameof(maxParticipants));

        return new Tournament
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerId,
            Name = name,
            Description = description,
            Format = format,
            Status = TournamentStatus.Registration,
            GameType = gameType,
            StartingScore = startingScore,
            LegsToWin = legsToWin,
            SetsToWin = setsToWin,
            MaxParticipants = maxParticipants,
            IsPublic = isPublic,
            JoinCode = isPublic ? null : GenerateJoinCode(),
            CreatedAt = DateTime.UtcNow,
            ScheduledStartAt = scheduledStartAt
        };
    }

    public void AddParticipant(TournamentParticipant participant)
    {
        if (Status != TournamentStatus.Registration)
            throw new InvalidOperationException("Cannot join tournament - registration is closed");
        
        if (Participants.Count >= MaxParticipants)
            throw new InvalidOperationException("Tournament is full");
        
        if (Participants.Any(p => p.UserId == participant.UserId))
            throw new InvalidOperationException("User is already registered");
        
        Participants.Add(participant);
    }

    public void RemoveParticipant(Guid userId)
    {
        if (Status != TournamentStatus.Registration)
            throw new InvalidOperationException("Cannot leave tournament after registration closes");
        
        var participant = Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            Participants.Remove(participant);
        }
    }

    public void CloseRegistration()
    {
        if (Status != TournamentStatus.Registration)
            throw new InvalidOperationException("Tournament is not in registration phase");
        
        if (Participants.Count < MinParticipants)
            throw new InvalidOperationException($"Need at least {MinParticipants} participants to start");
        
        Status = TournamentStatus.Scheduled;
    }

    public void Start()
    {
        if (Status != TournamentStatus.Scheduled)
            throw new InvalidOperationException("Tournament must be in scheduled status to start");
        
        Status = TournamentStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(Guid winnerId)
    {
        if (Status != TournamentStatus.InProgress)
            throw new InvalidOperationException("Tournament must be in progress to complete");
        
        Status = TournamentStatus.Completed;
        WinnerId = winnerId;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == TournamentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed tournament");
        
        Status = TournamentStatus.Cancelled;
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
