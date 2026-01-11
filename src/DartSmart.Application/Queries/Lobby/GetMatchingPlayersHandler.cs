using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;

namespace DartSmart.Application.Queries.Lobby;

public sealed class GetMatchingPlayersHandler : IRequestHandler<GetMatchingPlayersQuery, Result<List<LobbyPlayerDto>>>
{
    private readonly ILobbyRepository _lobbyRepository;

    public GetMatchingPlayersHandler(ILobbyRepository lobbyRepository)
    {
        _lobbyRepository = lobbyRepository;
    }

    public async Task<Result<List<LobbyPlayerDto>>> Handle(GetMatchingPlayersQuery request, CancellationToken cancellationToken)
    {
        var playerId = PlayerId.From(Guid.Parse(request.PlayerId));
        var players = await _lobbyRepository.GetMatchingPlayersAsync(playerId, request.AvgTolerance, cancellationToken);

        var lobbyPlayers = players.Select(p => new LobbyPlayerDto(
            p.Id.Value.ToString(),
            p.Username,
            p.Statistics.Average3Dart,
            DateTime.UtcNow
        )).ToList();

        return Result<List<LobbyPlayerDto>>.Success(lobbyPlayers);
    }
}
