using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.DTOs.Game;

/// <summary>
/// Represents a player setup for creating a new game
/// </summary>
public sealed record PlayerSetupDto(
    /// <summary>
    /// User ID for registered players, null for guests/bots
    /// </summary>
    Guid? UserId,
    
    /// <summary>
    /// Type of player (Human, Guest, Bot)
    /// </summary>
    PlayerType PlayerType,
    
    /// <summary>
    /// Custom display name (optional for registered users, required for guests/bots)
    /// </summary>
    string? DisplayName = null
);
