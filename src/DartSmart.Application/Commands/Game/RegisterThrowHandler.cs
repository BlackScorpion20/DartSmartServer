using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Application.Commands.Game;

public sealed class RegisterThrowHandler : IRequestHandler<RegisterThrowCommand, Result<DartThrowDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IDartThrowRepository _dartThrowRepository;
    private readonly Services.BotHostedService _botService;
    private readonly IPlayerRepository _playerRepository; // Need to check if next player is bot

    public RegisterThrowHandler(
        IGameRepository gameRepository, 
        IDartThrowRepository dartThrowRepository,
        Services.BotHostedService botService,
        IPlayerRepository playerRepository)
    {
        _gameRepository = gameRepository;
        _dartThrowRepository = dartThrowRepository;
        _botService = botService;
        _playerRepository = playerRepository;
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
            
            // If turn changed or game flow, we need to check if next player is a bot.
            // But RegisterThrow handles logic.
            // Game logic: After a throw, `game.CurrentPlayerIndex` might have changed if turn ended.
            // We should check who is the current player NOW.
            
            await _gameRepository.UpdateAsync(game, cancellationToken);

            // Check if it's now a bot's turn
            if (game.Status == GameStatus.InProgress)
            {
                var currentPlayer = game.GetCurrentPlayer();
                if (currentPlayer != null)
                {
                    // Optimization: We could load players with game, or fetch here.
                    // Let's fetch to be safe and separate.
                    var player = await _playerRepository.GetByIdAsync(currentPlayer.PlayerId, cancellationToken);
                    if (player != null && player.IsBot)
                    {
                        _botService.QueueBotTurn(game.Id);
                    }
                }
            }

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
