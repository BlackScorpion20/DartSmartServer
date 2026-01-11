using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Commands.Game;

public sealed class JoinGameHandler : IRequestHandler<JoinGameCommand, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;

    public JoinGameHandler(IGameRepository gameRepository, IPlayerRepository playerRepository)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
    }

    public async Task<Result<GameDto>> Handle(JoinGameCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(Guid.Parse(request.GameId));
        var playerId = PlayerId.From(Guid.Parse(request.PlayerId));

        var game = await _gameRepository.GetByIdWithPlayersAsync(gameId, cancellationToken);
        if (game is null)
            return Result<GameDto>.Failure("Game not found");

        var player = await _playerRepository.GetByIdAsync(playerId, cancellationToken);
        if (player is null)
            return Result<GameDto>.Failure("Player not found");

        try
        {
            game.AddPlayer(playerId);
            await _gameRepository.UpdateAsync(game, cancellationToken);
            
            var players = await GetAllGamePlayers(game, cancellationToken);
            return Result<GameDto>.Success(GameDtoMapper.Map(game, players));
        }
        catch (InvalidOperationException ex)
        {
            return Result<GameDto>.Failure(ex.Message);
        }
    }

    private async Task<List<Player>> GetAllGamePlayers(Domain.Entities.Game game, CancellationToken cancellationToken)
    {
        var players = new List<Player>();
        foreach (var gp in game.Players)
        {
            var p = await _playerRepository.GetByIdAsync(gp.PlayerId, cancellationToken);
            if (p != null) players.Add(p);
        }
        return players;
    }
}
