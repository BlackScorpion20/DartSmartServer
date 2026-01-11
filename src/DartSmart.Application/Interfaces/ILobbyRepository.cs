using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Application.Interfaces;

/// <summary>
/// Repository interface for matchmaking lobby
/// </summary>
public interface ILobbyRepository
{
    Task<IReadOnlyList<Player>> GetPlayersInLobbyAsync(CancellationToken cancellationToken = default);
    Task AddPlayerToLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task RemovePlayerFromLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<bool> IsPlayerInLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get players matching within avg tolerance
    /// </summary>
    Task<IReadOnlyList<Player>> GetMatchingPlayersAsync(
        PlayerId playerId, 
        decimal avgTolerance = 10, 
        CancellationToken cancellationToken = default);
}
