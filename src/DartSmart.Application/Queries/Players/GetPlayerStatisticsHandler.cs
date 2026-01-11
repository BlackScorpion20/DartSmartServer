using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;

namespace DartSmart.Application.Queries.Players;

public sealed class GetPlayerStatisticsHandler : IRequestHandler<GetPlayerStatisticsQuery, Result<PlayerStatisticsDto>>
{
    private readonly IPlayerRepository _playerRepository;

    public GetPlayerStatisticsHandler(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<Result<PlayerStatisticsDto>> Handle(GetPlayerStatisticsQuery request, CancellationToken cancellationToken)
    {
        var playerId = PlayerId.From(Guid.Parse(request.PlayerId));
        var player = await _playerRepository.GetByIdAsync(playerId, cancellationToken);

        if (player is null)
            return Result<PlayerStatisticsDto>.Failure("Player not found");

        return Result<PlayerStatisticsDto>.Success(new PlayerStatisticsDto(
            player.Statistics.TotalGames,
            player.Statistics.Wins,
            player.Statistics.Best3DartScore,
            player.Statistics.Count180s,
            player.Statistics.HighestCheckout,
            player.Statistics.TotalDarts,
            player.Statistics.TotalPoints,
            player.Statistics.WinRate,
            player.Statistics.AveragePerDart,
            player.Statistics.Average3Dart
        ));
    }
}
