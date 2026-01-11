using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Shouldly;

namespace DartSmart.Domain.Tests.Entities;

public class GameTests
{
    [Fact]
    public void Create_WithValidParams_ShouldCreateGame()
    {
        // Arrange & Act
        var game = Game.Create(GameType.X01_501, startScore: 501, X01InMode.StraightIn, X01OutMode.DoubleOut);

        // Assert
        game.ShouldNotBeNull();
        game.GameType.ShouldBe(GameType.X01_501);
        game.StartScore.ShouldBe(501);
        game.InMode.ShouldBe(X01InMode.StraightIn);
        game.OutMode.ShouldBe(X01OutMode.DoubleOut);
        game.Status.ShouldBe(GameStatus.Lobby);
    }

    [Fact]
    public void Create_ShouldHaveEmptyPlayersList()
    {
        // Arrange & Act
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);

        // Assert
        game.Players.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldHaveEmptyThrowsList()
    {
        // Arrange & Act
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);

        // Assert
        game.Throws.ShouldBeEmpty();
    }

    [Fact]
    public void AddPlayer_ShouldAddPlayerToGame()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();

        // Act
        game.AddPlayer(playerId);

        // Assert
        game.Players.Count.ShouldBe(1);
        game.Players.First().PlayerId.ShouldBe(playerId);
    }

    [Fact]
    public void AddPlayer_ShouldSetCorrectTurnOrder()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var player1 = PlayerId.New();
        var player2 = PlayerId.New();

        // Act
        game.AddPlayer(player1);
        game.AddPlayer(player2);

        // Assert
        game.Players.First(p => p.PlayerId == player1).TurnOrder.ShouldBe(0);
        game.Players.First(p => p.PlayerId == player2).TurnOrder.ShouldBe(1);
    }

    [Fact]
    public void AddPlayer_ShouldSetStartScore()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();

        // Act
        game.AddPlayer(playerId);

        // Assert
        game.Players.First().CurrentScore.ShouldBe(501);
    }

    [Fact]
    public void AddPlayer_WhenGameInProgress_ShouldThrow()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        game.AddPlayer(PlayerId.New());
        game.AddPlayer(PlayerId.New());
        game.Start();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => game.AddPlayer(PlayerId.New()));
    }

    [Fact]
    public void AddPlayer_WhenPlayerAlreadyInGame_ShouldThrow()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => game.AddPlayer(playerId));
    }

    [Fact]
    public void Start_WithEnoughPlayers_ShouldSetStatusToInProgress()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        game.AddPlayer(PlayerId.New());
        game.AddPlayer(PlayerId.New());

        // Act
        game.Start();

        // Assert
        game.Status.ShouldBe(GameStatus.InProgress);
    }

    [Fact]
    public void Start_WithNoPlayers_ShouldThrow()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => game.Start());
    }

    [Fact]
    public void Start_WithOnePlayer_ShouldSucceed()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        game.AddPlayer(PlayerId.New());

        // Act
        game.Start();

        // Assert - Game can be started with one player (e.g., practice mode)
        game.Status.ShouldBe(GameStatus.InProgress);
    }

    [Fact]
    public void Start_ShouldSetCurrentPlayerToFirst()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        game.AddPlayer(PlayerId.New());
        game.AddPlayer(PlayerId.New());

        // Act
        game.Start();

        // Assert
        game.CurrentPlayerIndex.ShouldBe(0);
        game.CurrentRound.ShouldBe(1);
    }

    [Fact]
    public void RegisterThrow_ShouldDeductPointsFromPlayer()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);
        game.AddPlayer(PlayerId.New());
        game.Start();

        // Act
        game.RegisterThrow(playerId, segment: 20, multiplier: 3, dartNumber: 1); // 60 points

        // Assert
        game.Players.First(p => p.PlayerId == playerId).CurrentScore.ShouldBe(441); // 501 - 60
    }

    [Fact]
    public void RegisterThrow_ShouldAddToThrowsList()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);
        game.AddPlayer(PlayerId.New());
        game.Start();

        // Act
        game.RegisterThrow(playerId, segment: 20, multiplier: 1, dartNumber: 1);

        // Assert
        game.Throws.Count.ShouldBe(1);
    }

    [Fact]
    public void RegisterThrow_WhenNotPlayersTurn_ShouldThrow()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var player1 = PlayerId.New();
        var player2 = PlayerId.New();
        game.AddPlayer(player1);
        game.AddPlayer(player2);
        game.Start();

        // Act & Assert - player2 tries to throw when it's player1's turn
        Should.Throw<InvalidOperationException>(() => 
            game.RegisterThrow(player2, segment: 20, multiplier: 1, dartNumber: 1));
    }

    [Fact]
    public void RegisterThrow_WhenGameNotStarted_ShouldThrow()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501, X01InMode.StraightIn, X01OutMode.DoubleOut);
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => 
            game.RegisterThrow(playerId, segment: 20, multiplier: 1, dartNumber: 1));
    }

    [Theory]
    [InlineData(GameType.X01_301, 301)]
    [InlineData(GameType.X01_501, 501)]
    [InlineData(GameType.X01_701, 701)]
    public void Create_WithDifferentGameTypes_ShouldSetCorrectStartScore(GameType gameType, int expectedScore)
    {
        // Arrange & Act
        var game = Game.Create(gameType, expectedScore, X01InMode.StraightIn, X01OutMode.DoubleOut);

        // Assert
        game.StartScore.ShouldBe(expectedScore);
        game.GameType.ShouldBe(gameType);
    }
}
