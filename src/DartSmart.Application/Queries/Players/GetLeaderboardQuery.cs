using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Queries.Players;

public record GetLeaderboardQuery(LeaderboardType Type, int Count = 10) : IRequest<Result<List<LeaderboardEntryDto>>>;
