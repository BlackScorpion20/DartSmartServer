using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;

namespace DartSmart.Application.Commands.Game;

public sealed class RegisterThrowHandler : IRequestHandler<RegisterThrowCommand, Result<DartThrowDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IDartThrowRepository _dartThrowRepository;

    public RegisterThrowHandler(IGameRepository gameRepository, IDartThrowRepository dartThrowRepository)
    {
        _gameRepository = gameRepository;
        _dartThrowRepository = dartThrowRepository;
    }

    public async Task<Result<DartThrowDto>> Handle(RegisterThrowCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(Guid.Parse(request.GameId));
        var playerId = PlayerId.From(Guid.Parse(request.PlayerId));

        var game = await _gameRepository.GetByIdWithPlayersAsync(gameId, cancellationToken);
        if (game is null)
            return Result<DartThrowDto>.Failure("Game not found");

        try
        {
            var dartThrow = game.RegisterThrow(playerId, request.Segment, request.Multiplier, request.DartNumber);
            
            await _dartThrowRepository.AddAsync(dartThrow, cancellationToken);
            await _gameRepository.UpdateAsync(game, cancellationToken);

            return Result<DartThrowDto>.Success(new DartThrowDto(
                dartThrow.Id.Value.ToString(),
                dartThrow.GameId.Value.ToString(),
                dartThrow.PlayerId.Value.ToString(),
                dartThrow.Segment,
                dartThrow.Multiplier,
                dartThrow.Points,
                dartThrow.Round,
                dartThrow.DartNumber,
                dartThrow.Timestamp,
                dartThrow.IsBust
            ));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result<DartThrowDto>.Failure(ex.Message);
        }
    }
}
