using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Queries.Lobby;

public record GetMatchingPlayersQuery(string PlayerId, decimal AvgTolerance = 10) 
    : IRequest<Result<List<LobbyPlayerDto>>>;
