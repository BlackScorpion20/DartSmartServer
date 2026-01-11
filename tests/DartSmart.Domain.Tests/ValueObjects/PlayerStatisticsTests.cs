using DartSmart.Domain.ValueObjects;
using Shouldly;

namespace DartSmart.Domain.Tests.ValueObjects;

public class PlayerStatisticsTests
{
    [Fact]
    public void Empty_ShouldInitializeAllToZero()
    {
        // Arrange & Act
        var stats = PlayerStatistics.Empty;

        // Assert
        stats.TotalGames.ShouldBe(0);
        stats.Wins.ShouldBe(0);
        stats.Best3DartScore.ShouldBe(0);
        stats.Count180s.ShouldBe(0);
        stats.HighestCheckout.ShouldBe(0);
        stats.TotalDarts.ShouldBe(0);
        stats.TotalPoints.ShouldBe(0);
    }

    [Fact]
    public void WinRate_WithNoGames_ShouldReturnZero()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Assert
        stats.WinRate.ShouldBe(0);
    }

    [Fact]
    public void WinRate_ShouldCalculatePercentageCorrectly()
    {
        // Arrange
        var stats = PlayerStatistics.Empty with { TotalGames = 100, Wins = 75 };

        // Assert
        stats.WinRate.ShouldBe(75m);
    }

    [Fact]
    public void AveragePerDart_WithNoDarts_ShouldReturnZero()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Assert
        stats.AveragePerDart.ShouldBe(0);
    }

    [Fact]
    public void AveragePerDart_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = PlayerStatistics.Empty with { TotalDarts = 100, TotalPoints = 2500 };

        // Assert
        stats.AveragePerDart.ShouldBe(25m); // 2500 / 100 = 25
    }

    [Fact]
    public void Average3Dart_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = PlayerStatistics.Empty with { TotalDarts = 100, TotalPoints = 3000 };

        // Assert
        stats.Average3Dart.ShouldBe(90m); // (3000 / 100) * 3 = 90
    }

    [Fact]
    public void WithGame_Win_ShouldIncrementWinsAndGames()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act
        var updated = stats.WithGame(isWin: true, dartsThrown: 15, pointsScored: 501);

        // Assert
        updated.TotalGames.ShouldBe(1);
        updated.Wins.ShouldBe(1);
        updated.TotalDarts.ShouldBe(15);
        updated.TotalPoints.ShouldBe(501);
    }

    [Fact]
    public void WithGame_Loss_ShouldNotIncrementWins()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act
        var updated = stats.WithGame(isWin: false, dartsThrown: 20, pointsScored: 400);

        // Assert
        updated.TotalGames.ShouldBe(1);
        updated.Wins.ShouldBe(0);
    }

    [Fact]
    public void WithGame_WithCheckout_ShouldUpdateHighestCheckout()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act
        var updated = stats.WithGame(isWin: true, dartsThrown: 15, pointsScored: 501, checkoutScore: 120);

        // Assert
        updated.HighestCheckout.ShouldBe(120);
    }

    [Fact]
    public void WithGame_WithLowerCheckout_ShouldNotUpdateHighestCheckout()
    {
        // Arrange
        var stats = PlayerStatistics.Empty with { HighestCheckout = 150 };

        // Act
        var updated = stats.WithGame(isWin: true, dartsThrown: 15, pointsScored: 501, checkoutScore: 100);

        // Assert
        updated.HighestCheckout.ShouldBe(150);
    }

    [Fact]
    public void With180_ShouldIncrement180Count()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act
        var updated = stats.With180();

        // Assert
        updated.Count180s.ShouldBe(1);
    }

    [Fact]
    public void With180_Multiple_ShouldAccumulate()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act
        var updated = stats.With180().With180().With180();

        // Assert
        updated.Count180s.ShouldBe(3);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var stats1 = PlayerStatistics.Empty with { TotalGames = 10 };
        var stats2 = PlayerStatistics.Empty with { TotalGames = 10 };

        // Assert
        stats1.ShouldBe(stats2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var stats1 = PlayerStatistics.Empty with { TotalGames = 10 };
        var stats2 = PlayerStatistics.Empty with { TotalGames = 11 };

        // Assert
        stats1.ShouldNotBe(stats2);
    }

    [Fact]
    public void WithGame_ShouldCalculateBest3DartScore()
    {
        // Arrange
        var stats = PlayerStatistics.Empty;

        // Act - 180 points in 3 darts = 180/3*3 = 180
        var updated = stats.WithGame(isWin: false, dartsThrown: 3, pointsScored: 180);

        // Assert
        updated.Best3DartScore.ShouldBe(180);
    }
}
