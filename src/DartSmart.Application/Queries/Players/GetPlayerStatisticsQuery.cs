using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Queries.Players;

public record GetPlayerStatisticsQuery(string PlayerId) : IRequest<Result<PlayerStatisticsDto>>;
