using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Application.Services;

namespace DartSmart.Application.Commands.Game;

public sealed class StartGameHandler : IRequestHandler<StartGameCommand, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly Services.BotHostedService _botService;

    public StartGameHandler(
        IGameRepository gameRepository, 
        IPlayerRepository playerRepository,
        Services.BotHostedService botService)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
        _botService = botService;
    }

    public async Task<Result<GameDto>> Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(Guid.Parse(request.GameId));
        var game = await _gameRepository.GetByIdWithPlayersAsync(gameId, cancellationToken);
        
        if (game is null)
            return Result<GameDto>.Failure("Game not found");

        try
        {
            game.Start();
            await _gameRepository.UpdateAsync(game, cancellationToken);
            
            // Should be the first player
            var currentPlayer = game.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                var player = await _playerRepository.GetByIdAsync(currentPlayer.PlayerId, cancellationToken);
                if (player != null && player.IsBot)
                {
                    _botService.QueueBotTurn(game.Id);
                }
            }

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
