using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Commands.Game;

public sealed class CreateGameHandler : IRequestHandler<CreateGameCommand, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;

    public CreateGameHandler(IGameRepository gameRepository, IPlayerRepository playerRepository)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
    }

    public async Task<Result<GameDto>> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var playerId = PlayerId.From(Guid.Parse(request.PlayerId));
        var player = await _playerRepository.GetByIdAsync(playerId, cancellationToken);
        
        if (player is null)
            return Result<GameDto>.Failure("Player not found");

        var game = Domain.Entities.Game.Create(request.GameType, request.StartScore, request.InMode, request.OutMode);
        game.AddPlayer(playerId);

        await _gameRepository.AddAsync(game, cancellationToken);

        return Result<GameDto>.Success(GameDtoMapper.Map(game, new[] { player }));
    }
}
