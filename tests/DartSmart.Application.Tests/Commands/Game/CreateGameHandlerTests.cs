using DartSmart.Application.Commands.Game;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using NSubstitute;
using Shouldly;

namespace DartSmart.Application.Tests.Commands.Game;

public class CreateGameHandlerTests
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly CreateGameHandler _handler;

    public CreateGameHandlerTests()
    {
        _gameRepository = Substitute.For<IGameRepository>();
        _playerRepository = Substitute.For<IPlayerRepository>();
        _handler = new CreateGameHandler(_gameRepository, _playerRepository);
    }

    [Fact]
    public async Task Handle_WithValidPlayer_ShouldReturnSuccess()
    {
        // Arrange
        var playerId = PlayerId.New();
        var player = Player.Create("TestUser", "test@example.com", "hash");
        var command = new CreateGameCommand(playerId.Value.ToString(), GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns(player);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.GameType.ShouldBe("X01_501");
        result.Value.StartScore.ShouldBe(501);
    }

    [Fact]
    public async Task Handle_WithNonExistentPlayer_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateGameCommand(Guid.NewGuid().ToString(), GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Player not found");
    }

    [Fact]
    public async Task Handle_ShouldAddGameToRepository()
    {
        // Arrange
        var playerId = PlayerId.New();
        var player = Player.Create("TestUser", "test@example.com", "hash");
        var command = new CreateGameCommand(playerId.Value.ToString(), GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns(player);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _gameRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.Game>(g => g.StartScore == 501), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAddCreatorAsFirstPlayer()
    {
        // Arrange
        var playerId = PlayerId.New();
        var player = Player.Create("TestUser", "test@example.com", "hash");
        var command = new CreateGameCommand(playerId.Value.ToString(), GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns(player);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.Players.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(301)]
    [InlineData(501)]
    [InlineData(701)]
    public async Task Handle_WithDifferentScores_ShouldCreateGameWithCorrectScore(int startScore)
    {
        // Arrange
        var playerId = PlayerId.New();
        var player = Player.Create("TestUser", "test@example.com", "hash");
        var command = new CreateGameCommand(playerId.Value.ToString(), GameType.X01_501, startScore, X01InMode.StraightIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns(player);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.StartScore.ShouldBe(startScore);
    }

    [Fact]
    public async Task Handle_WithDoubleIn_ShouldSetCorrectMode()
    {
        // Arrange
        var playerId = PlayerId.New();
        var player = Player.Create("TestUser", "test@example.com", "hash");
        var command = new CreateGameCommand(playerId.Value.ToString(), GameType.X01_501, 501, X01InMode.DoubleIn, X01OutMode.DoubleOut);
        
        _playerRepository.GetByIdAsync(Arg.Any<PlayerId>(), Arg.Any<CancellationToken>()).Returns(player);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.InMode.ShouldBe("DoubleIn");
    }
}
