using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartSmart.Infrastructure.Persistence.Repositories;

public class LobbyRepository : ILobbyRepository
{
    private readonly DartSmartDbContext _context;
    private static readonly HashSet<PlayerId> _lobbyPlayers = new();
    private static readonly object _lock = new();

    public LobbyRepository(DartSmartDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Player>> GetPlayersInLobbyAsync(CancellationToken cancellationToken = default)
    {
        List<PlayerId> playerIds;
        lock (_lock)
        {
            playerIds = _lobbyPlayers.ToList();
        }

        if (playerIds.Count == 0)
            return Array.Empty<Player>();

        return await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public Task AddPlayerToLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _lobbyPlayers.Add(playerId);
        }
        return Task.CompletedTask;
    }

    public Task RemovePlayerFromLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _lobbyPlayers.Remove(playerId);
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsPlayerInLobbyAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_lobbyPlayers.Contains(playerId));
        }
    }

    public async Task<IReadOnlyList<Player>> GetMatchingPlayersAsync(
        PlayerId playerId, 
        decimal avgTolerance = 10, 
        CancellationToken cancellationToken = default)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
        if (player is null)
            return Array.Empty<Player>();

        var playerAvg = player.Statistics.Average3Dart;

        List<PlayerId> lobbyPlayerIds;
        lock (_lock)
        {
            lobbyPlayerIds = _lobbyPlayers.Where(id => id != playerId).ToList();
        }

        if (lobbyPlayerIds.Count == 0)
            return Array.Empty<Player>();

        var lobbyPlayers = await _context.Players
            .Where(p => lobbyPlayerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return lobbyPlayers
            .Where(p => Math.Abs(p.Statistics.Average3Dart - playerAvg) <= avgTolerance)
            .OrderBy(p => Math.Abs(p.Statistics.Average3Dart - playerAvg))
            .ToList();
    }
}
