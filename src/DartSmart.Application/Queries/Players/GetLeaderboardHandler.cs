using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;

namespace DartSmart.Application.Queries.Players;

public sealed class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, Result<List<LeaderboardEntryDto>>>
{
    private readonly IPlayerRepository _playerRepository;

    public GetLeaderboardHandler(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<Result<List<LeaderboardEntryDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var statName = request.Type switch
        {
            LeaderboardType.MostGames => "TotalGames",
            LeaderboardType.MostDarts => "TotalDarts",
            LeaderboardType.BestAverage => "Average3Dart",
            LeaderboardType.Most180s => "Count180s",
            LeaderboardType.HighestCheckout => "HighestCheckout",
            LeaderboardType.BestWinRate => "WinRate",
            _ => "TotalGames"
        };

        var players = await _playerRepository.GetTopByStatisticAsync(statName, request.Count, cancellationToken);

        var entries = players.Select((p, index) => new LeaderboardEntryDto(
            index + 1,
            p.Id.Value.ToString(),
            p.Username,
            request.Type switch
            {
                LeaderboardType.MostGames => p.Statistics.TotalGames,
                LeaderboardType.MostDarts => p.Statistics.TotalDarts,
                LeaderboardType.BestAverage => p.Statistics.Average3Dart,
                LeaderboardType.Most180s => p.Statistics.Count180s,
                LeaderboardType.HighestCheckout => p.Statistics.HighestCheckout,
                LeaderboardType.BestWinRate => p.Statistics.WinRate,
                _ => 0
            },
            request.Type.ToString()
        )).ToList();

        return Result<List<LeaderboardEntryDto>>.Success(entries);
    }
}
