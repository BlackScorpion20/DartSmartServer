using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Queries.Games;

public record GetActiveGamesQuery() : IRequest<Result<List<GameSummaryDto>>>;
