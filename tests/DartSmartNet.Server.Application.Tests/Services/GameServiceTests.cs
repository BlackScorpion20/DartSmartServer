using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DartSmartNet.Server.Application.Tests.Services;

public class GameServiceTests
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStatisticsService _statisticsService;
    private readonly GameService _sut;

    public GameServiceTests()
    {
        _gameRepository = Substitute.For<IGameRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _statisticsService = Substitute.For<IStatisticsService>();
        _sut = new GameService(_gameRepository, _userRepository, _statisticsService);
    }

    [Fact]
    public async Task CreateGameAsync_WithValidX01Game_ShouldCreateGame()
    {
        // Arrange
        var gameType = GameType.X01;
        var startingScore = 501;
        var playerIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var user1 = User.Create("player1", "player1@test.com", "hash");
        var user2 = User.Create("player2", "player2@test.com", "hash");

        _userRepository.GetByIdAsync(playerIds[0], Arg.Any<CancellationToken>())
            .Returns(user1);
        _userRepository.GetByIdAsync(playerIds[1], Arg.Any<CancellationToken>())
            .Returns(user2);

        // Act
        var result = await _sut.CreateGameAsync(gameType, startingScore, playerIds, isOnline: true);

        // Assert
        result.ShouldNotBeNull();
        result.GameType.ShouldBe(GameType.X01);
        result.StartingScore.ShouldBe(startingScore);
        result.Players.Count.ShouldBe(2);
        result.Status.ShouldBe(GameStatus.WaitingForPlayers);
        result.IsOnline.ShouldBeTrue();

        await _gameRepository.Received(1).AddAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGameAsync_WithX01GameWithoutStartingScore_ShouldThrowException()
    {
        // Arrange
        var gameType = GameType.X01;
        int? startingScore = null;
        var playerIds = new[] { Guid.NewGuid() };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.CreateGameAsync(gameType, startingScore, playerIds, isOnline: false));
    }

    [Fact]
    public async Task StartGameAsync_WithWaitingGame_ShouldStartGame()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = GameSession.Create(GameType.X01, 501, isOnline: false, isBotGame: false);
        game.AddPlayer(Guid.NewGuid(), 0);
        game.AddPlayer(Guid.NewGuid(), 1);

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns(game);

        var user1 = User.Create("player1", "player1@test.com", "hash");
        var user2 = User.Create("player2", "player2@test.com", "hash");
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(user1, user2);

        // Act
        var result = await _sut.StartGameAsync(gameId);

        // Assert
        result.Status.ShouldBe(GameStatus.InProgress);
        await _gameRepository.Received(1).UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterThrowAsync_WithValidThrow_ShouldRegisterThrow()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = GameSession.Create(GameType.X01, 501, isOnline: false, isBotGame: false);
        game.AddPlayer(userId, 0);
        game.Start();

        var score = Score.Triple(20);

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns(game);

        var user = User.Create("player1", "player1@test.com", "hash");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.RegisterThrowAsync(gameId, userId, score);

        // Assert
        result.ShouldNotBeNull();
        await _gameRepository.Received(1).UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGameStateAsync_WithExistingGame_ShouldReturnGameState()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = GameSession.Create(GameType.X01, 501, isOnline: false, isBotGame: false);
        game.AddPlayer(Guid.NewGuid(), 0);

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns(game);

        var user = User.Create("player1", "player1@test.com", "hash");
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.GetGameStateAsync(gameId);

        // Assert
        result.ShouldNotBeNull();
        result.GameType.ShouldBe(GameType.X01);
        result.StartingScore.ShouldBe(501);
        result.Players.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetGameStateAsync_WithNonExistingGame_ShouldThrowException()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns((GameSession?)null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.GetGameStateAsync(gameId));
    }

    [Fact]
    public async Task EndGameAsync_WithInProgressGame_ShouldEndGame()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = GameSession.Create(GameType.X01, 501, isOnline: false, isBotGame: false);
        game.AddPlayer(userId, 0);
        game.Start();

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns(game);

        var user = User.Create("player1", "player1@test.com", "hash");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.EndGameAsync(gameId);

        // Assert
        result.Status.ShouldBeOneOf(GameStatus.Completed, GameStatus.Abandoned);
        await _gameRepository.Received(1).UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserGamesAsync_ShouldReturnUserGames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var games = new List<GameSession>
        {
            GameSession.Create(GameType.X01, 501, isOnline: false, isBotGame: false),
            GameSession.Create(GameType.X01, 301, isOnline: false, isBotGame: false)
        };

        games[0].AddPlayer(userId, 0);
        games[1].AddPlayer(userId, 0);

        _gameRepository.GetUserGamesAsync(userId, 20, Arg.Any<CancellationToken>())
            .Returns(games);

        var user = User.Create("player1", "player1@test.com", "hash");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.GetUserGamesAsync(userId);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count().ShouldBe(2);
    }
}
