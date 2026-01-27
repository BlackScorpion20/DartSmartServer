using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Application.Services;

/// <summary>
/// Service for handling bot turns in games
/// </summary>
public interface IBotTurnService
{
    /// <summary>
    /// Checks if the next player in the game is a bot and should take their turn
    /// </summary>
    Task<bool> ShouldBotPlayNextAsync(Guid gameId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a full bot turn (3 darts or until bust/win) for the current bot player
    /// Returns the updated game state after each throw for broadcasting
    /// </summary>
    IAsyncEnumerable<(GameStateDto State, int Segment, int Multiplier, int Points)> ExecuteBotTurnAsync(
        Guid gameId, 
        CancellationToken cancellationToken = default);
}
