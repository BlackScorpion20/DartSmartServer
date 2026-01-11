using System.Threading.Channels;
using DartSmart.Application.Commands.Game;
using DartSmart.Application.Common;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Bots;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DartSmart.Application.Services;

public class BotHostedService : BackgroundService
{
    private readonly Channel<GameId> _gameChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotHostedService> _logger;
    private readonly Random _random = new Random();

    public BotHostedService(
        IServiceProvider serviceProvider,
        ILogger<BotHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _gameChannel = Channel.CreateUnbounded<GameId>();
    }

    public void QueueBotTurn(GameId gameId)
    {
        _gameChannel.Writer.TryWrite(gameId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BotHostedService started.");

        await foreach (var gameId in _gameChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Configurable realistic delay (2-4 seconds)
                var delayMs = _random.Next(2000, 4001);
                await Task.Delay(delayMs, stoppingToken);

                await ProcessBotTurnAsync(gameId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bot turn for game {GameId}", gameId);
            }
        }
    }

    private async Task ProcessBotTurnAsync(GameId gameId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null || game.Status != GameStatus.InProgress) return;

        var currentPlayerInfo = game.GetCurrentPlayer();
        if (currentPlayerInfo == null) return;

        var player = await playerRepository.GetByIdAsync(currentPlayerInfo.PlayerId, cancellationToken);
        if (player == null || !player.IsBot) return; // Verify it's still a bot turn

        _logger.LogInformation("Processing bot turn for {BotName} (Skill: {SkillLevel}) in game {GameId}", 
            player.Username, player.BotSkillLevel, gameId);

        // Calculate Throws
        var strategy = new GaussianBotStrategy(player.BotSkillLevel ?? 50);
        var throws = strategy.CalculateTurn(game, player.Id);

        // Execute Throws via Command
        foreach (var t in throws)
        {
            var command = new RegisterThrowCommand(
                gameId.ToString(),
                player.Id.ToString(),
                t.Segment,
                t.Multiplier,
                t.DartNumber
            );

            // Execute synchronously to ensure order
            await mediator.Send(command, cancellationToken);
            
            // Small delay between darts for realism? (Optional, maybe 500ms)
            await Task.Delay(500, cancellationToken);
        }
    }
}
