using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;

namespace DartSmart.Application.Queries.Games;

public sealed class GetActiveGamesHandler : IRequestHandler<GetActiveGamesQuery, Result<List<GameSummaryDto>>>
{
    private readonly IGameRepository _gameRepository;

    public GetActiveGamesHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<List<GameSummaryDto>>> Handle(GetActiveGamesQuery request, CancellationToken cancellationToken)
    {
        var games = await _gameRepository.GetActiveGamesAsync(cancellationToken);

        var summaries = games.Select(g => new GameSummaryDto(
            g.Id.Value.ToString(),
            g.GameType.ToString(),
            g.StartScore,
            g.Status.ToString(),
            g.Players.Count,
            g.CreatedAt
        )).ToList();

        return Result<List<GameSummaryDto>>.Success(summaries);
    }
}
