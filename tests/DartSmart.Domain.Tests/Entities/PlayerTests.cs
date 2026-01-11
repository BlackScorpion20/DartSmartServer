using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Shouldly;

namespace DartSmart.Domain.Tests.Entities;

public class PlayerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePlayer()
    {
        // Arrange & Act
        var player = Player.Create("TestPlayer", "test@example.com", "hashedPassword123");

        // Assert
        player.ShouldNotBeNull();
        player.Username.ShouldBe("TestPlayer");
        player.Email.ShouldBe("test@example.com");
        player.PasswordHash.ShouldBe("hashedPassword123");
        player.Id.ShouldNotBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeStatisticsToZero()
    {
        // Arrange & Act
        var player = Player.Create("TestPlayer", "test@example.com", "hash");

        // Assert
        player.Statistics.ShouldNotBeNull();
        player.Statistics.TotalGames.ShouldBe(0);
        player.Statistics.Wins.ShouldBe(0);
        player.Statistics.TotalDarts.ShouldBe(0);
        player.Statistics.TotalPoints.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var player1 = Player.Create("Player1", "test1@example.com", "hash");
        var player2 = Player.Create("Player2", "test2@example.com", "hash");

        // Assert
        player1.Id.ShouldNotBe(player2.Id);
    }

    [Fact]
    public void Create_ShouldNormalizeEmail()
    {
        // Arrange & Act
        var player = Player.Create("Test", "TEST@Example.COM", "hash");

        // Assert
        player.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void Create_ShouldTrimUsername()
    {
        // Arrange & Act
        var player = Player.Create("  TestPlayer  ", "test@example.com", "hash");

        // Assert
        player.Username.ShouldBe("TestPlayer");
    }

    [Fact]
    public void Create_WithEmptyUsername_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => 
            Player.Create("", "test@example.com", "hash"));
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => 
            Player.Create("TestPlayer", "", "hash"));
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => 
            Player.Create("TestPlayer", "invalidemail", "hash"));
    }

    [Fact]
    public void UpdateStatistics_ShouldSetNewStatistics()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com", "hash");
        var newStats = player.Statistics with { TotalGames = 10, Wins = 5 };

        // Act
        player.UpdateStatistics(newStats);

        // Assert
        player.Statistics.TotalGames.ShouldBe(10);
        player.Statistics.Wins.ShouldBe(5);
    }

    [Fact]
    public void UpdateStatistics_WithNull_ShouldThrow()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com", "hash");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => player.UpdateStatistics(null!));
    }

    [Fact]
    public void RecordGameResult_AsWin_ShouldUpdateStats()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com", "hash");

        // Act
        player.RecordGameResult(isWin: true, dartsThrown: 15, pointsScored: 501, checkoutScore: 40);

        // Assert
        player.Statistics.TotalGames.ShouldBe(1);
        player.Statistics.Wins.ShouldBe(1);
        player.Statistics.TotalDarts.ShouldBe(15);
        player.Statistics.HighestCheckout.ShouldBe(40);
    }

    [Fact]
    public void RecordGameResult_AsLoss_ShouldNotIncrementWins()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com", "hash");

        // Act
        player.RecordGameResult(isWin: false, dartsThrown: 20, pointsScored: 400);

        // Assert
        player.Statistics.TotalGames.ShouldBe(1);
        player.Statistics.Wins.ShouldBe(0);
    }

    [Fact]
    public void Record180_ShouldIncrement180Count()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com", "hash");

        // Act
        player.Record180();
        player.Record180();

        // Assert
        player.Statistics.Count180s.ShouldBe(2);
    }

    [Fact]
    public void ChangeUsername_ShouldUpdateUsername()
    {
        // Arrange
        var player = Player.Create("OldName", "test@example.com", "hash");

        // Act
        player.ChangeUsername("NewName");

        // Assert
        player.Username.ShouldBe("NewName");
    }

    [Fact]
    public void ChangeUsername_WithEmpty_ShouldThrow()
    {
        // Arrange
        var player = Player.Create("Test", "test@example.com", "hash");

        // Act & Assert
        Should.Throw<ArgumentException>(() => player.ChangeUsername(""));
    }

    [Fact]
    public void UpdatePasswordHash_ShouldChangePassword()
    {
        // Arrange
        var player = Player.Create("Test", "test@example.com", "oldHash");

        // Act
        player.UpdatePasswordHash("newHash");

        // Assert
        player.PasswordHash.ShouldBe("newHash");
    }

    [Fact]
    public void UpdatePasswordHash_WithEmpty_ShouldThrow()
    {
        // Arrange
        var player = Player.Create("Test", "test@example.com", "hash");

        // Act & Assert
        Should.Throw<ArgumentException>(() => player.UpdatePasswordHash(""));
    }
}
