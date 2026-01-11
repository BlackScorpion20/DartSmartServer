using DartSmart.Domain.Entities;
using DartSmart.Domain.Common;
using DartSmart.Domain.ValueObjects;
using Shouldly;

namespace DartSmart.Domain.Tests;

public class PlayerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePlayer()
    {
        // Act
        var player = Player.Create("TestUser", "test@example.com", "hashedpassword");

        // Assert
        player.ShouldNotBeNull();
        player.Username.ShouldBe("TestUser");
        player.Email.ShouldBe("test@example.com");
        player.PasswordHash.ShouldBe("hashedpassword");
        player.Id.ShouldNotBeNull();
        player.Statistics.ShouldNotBeNull();
        player.Statistics.TotalGames.ShouldBe(0);
    }

    [Fact]
    public void Create_WithEmptyUsername_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            Player.Create("", "test@example.com", "hash"));
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            Player.Create("user", "invalidemail", "hash"));
    }

    [Fact]
    public void RecordGameResult_ShouldUpdateStatistics()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "hash");

        // Act
        player.RecordGameResult(isWin: true, dartsThrown: 15, pointsScored: 501, checkoutScore: 36);

        // Assert
        player.Statistics.TotalGames.ShouldBe(1);
        player.Statistics.Wins.ShouldBe(1);
        player.Statistics.TotalDarts.ShouldBe(15);
        player.Statistics.TotalPoints.ShouldBe(501);
        player.Statistics.HighestCheckout.ShouldBe(36);
    }
}

public class GameTests
{
    [Fact]
    public void Create_X01_ShouldCreateGameInLobby()
    {
        // Act
        var game = Game.Create(GameType.X01_501, 501);

        // Assert
        game.ShouldNotBeNull();
        game.GameType.ShouldBe(GameType.X01_501);
        game.StartScore.ShouldBe(501);
        game.Status.ShouldBe(GameStatus.Lobby);
        game.OutMode.ShouldBe(X01OutMode.DoubleOut);
    }

    [Fact]
    public void AddPlayer_ShouldAddPlayerToGame()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501);
        var playerId = PlayerId.New();

        // Act
        game.AddPlayer(playerId);

        // Assert
        game.Players.Count.ShouldBe(1);
        game.Players.First().PlayerId.ShouldBe(playerId);
        game.Players.First().CurrentScore.ShouldBe(501);
    }

    [Fact]
    public void Start_WithPlayers_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501);
        game.AddPlayer(PlayerId.New());

        // Act
        game.Start();

        // Assert
        game.Status.ShouldBe(GameStatus.InProgress);
        game.CurrentRound.ShouldBe(1);
    }

    [Fact]
    public void RegisterThrow_ShouldSubtractScore()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 501);
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);
        game.Start();

        // Act - T20 = 60 points
        var dartThrow = game.RegisterThrow(playerId, segment: 20, multiplier: 3, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(60);
        game.Players.First().CurrentScore.ShouldBe(441); // 501 - 60
    }

    [Fact]
    public void RegisterThrow_BustOnDoubleOut_ShouldMarkAsBust()
    {
        // Arrange
        var game = Game.Create(GameType.X01_501, 40); // Start at 40 for easy test
        var playerId = PlayerId.New();
        game.AddPlayer(playerId);
        game.Start();

        // Act - Try to checkout with single 20 (invalid for double out)
        var dartThrow = game.RegisterThrow(playerId, segment: 20, multiplier: 1, dartNumber: 1);

        // Assert - Should not be bust, score is now 20
        dartThrow.IsBust.ShouldBeFalse();
        game.Players.First().CurrentScore.ShouldBe(20);

        // Now try to finish with single 20 again (would be 0 but not double)
        var bustThrow = game.RegisterThrow(playerId, segment: 20, multiplier: 1, dartNumber: 2);
        bustThrow.IsBust.ShouldBeTrue(); // Can't checkout without double
    }
}

public class DartThrowTests
{
    [Fact]
    public void Create_T20_ShouldCalculate60Points()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();

        // Act
        var dartThrow = DartThrow.Create(gameId, playerId, segment: 20, multiplier: 3, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(60);
        dartThrow.IsTriple.ShouldBeTrue();
        dartThrow.Is180Contributor.ShouldBeTrue();
    }

    [Fact]
    public void Create_DoubleBull_ShouldCalculate50Points()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();

        // Act
        var dartThrow = DartThrow.Create(gameId, playerId, segment: 25, multiplier: 2, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(50);
        dartThrow.IsDoubleBull.ShouldBeTrue();
    }

    [Fact]
    public void Create_TripleBull_ShouldThrow()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();

        // Act & Assert - Triple Bull is not possible
        Should.Throw<ArgumentException>(() => 
            DartThrow.Create(gameId, playerId, segment: 25, multiplier: 3, round: 1, dartNumber: 1));
    }
}
