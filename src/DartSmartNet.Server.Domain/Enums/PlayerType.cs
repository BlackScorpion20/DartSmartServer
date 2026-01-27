namespace DartSmartNet.Server.Domain.Enums;

/// <summary>
/// Defines the type of player in a game session
/// </summary>
public enum PlayerType
{
    /// <summary>
    /// Registered user with an account
    /// </summary>
    Human = 0,
    
    /// <summary>
    /// Local player without login (guest)
    /// </summary>
    Guest = 1,
    
    /// <summary>
    /// AI-controlled player
    /// </summary>
    Bot = 2
}
