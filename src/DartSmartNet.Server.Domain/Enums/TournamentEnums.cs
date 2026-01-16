namespace DartSmartNet.Server.Domain.Enums;

/// <summary>
/// Tournament format determining bracket structure
/// </summary>
public enum TournamentFormat
{
    /// <summary>
    /// Single elimination - lose once and you're out
    /// </summary>
    SingleElimination,
    
    /// <summary>
    /// Double elimination - must lose twice to be eliminated
    /// </summary>
    DoubleElimination,
    
    /// <summary>
    /// Round robin - everyone plays everyone
    /// </summary>
    RoundRobin
}

/// <summary>
/// Current status of a tournament
/// </summary>
public enum TournamentStatus
{
    /// <summary>
    /// Tournament is open for registration
    /// </summary>
    Registration,
    
    /// <summary>
    /// Registration closed, tournament starting soon
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// Tournament is in progress
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Tournament has been completed
    /// </summary>
    Completed,
    
    /// <summary>
    /// Tournament was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Status of a tournament match
/// </summary>
public enum TournamentMatchStatus
{
    /// <summary>
    /// Waiting for players or previous matches
    /// </summary>
    Pending,
    
    /// <summary>
    /// Match is ready to be played
    /// </summary>
    Ready,
    
    /// <summary>
    /// Match is currently in progress
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Match has been completed
    /// </summary>
    Completed,
    
    /// <summary>
    /// Match was forfeited (walkover)
    /// </summary>
    Walkover
}
