using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Shouldly;

namespace DartSmart.Domain.Tests.Entities;

public class DartThrowTests
{
    private readonly GameId _gameId = GameId.New();
    private readonly PlayerId _playerId = PlayerId.New();

    [Fact]
    public void Create_WithValidSingleNumber_ShouldCreateDartThrow()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1);

        // Assert
        dartThrow.ShouldNotBeNull();
        dartThrow.Segment.ShouldBe(20);
        dartThrow.Multiplier.ShouldBe(1);
        dartThrow.Points.ShouldBe(20);
        dartThrow.Round.ShouldBe(1);
        dartThrow.DartNumber.ShouldBe(1);
        dartThrow.IsBust.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithDouble_ShouldCalculateDoublePoints()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 2, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(40); // 20 * 2 = 40
        dartThrow.IsDouble.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithTriple_ShouldCalculateTriplePoints()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 3, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(60); // 20 * 3 = 60
        dartThrow.IsTriple.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithBullseye_ShouldScore25()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 25, multiplier: 1, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(25);
        dartThrow.IsBull.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithDoubleBullseye_ShouldScore50()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 25, multiplier: 2, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(50);
        dartThrow.IsDoubleBull.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithMiss_ShouldScoreZero()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 0, multiplier: 1, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(26)]
    [InlineData(100)]
    public void Create_WithInvalidSegment_ShouldThrow(int invalidSegment)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => 
            DartThrow.Create(_gameId, _playerId, segment: invalidSegment, multiplier: 1, round: 1, dartNumber: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(10)]
    public void Create_WithInvalidMultiplier_ShouldThrow(int invalidMultiplier)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => 
            DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: invalidMultiplier, round: 1, dartNumber: 1));
    }

    [Fact]
    public void Create_WithTripleBullseye_ShouldThrowArgumentException()
    {
        // Bullseye (25) cannot be tripled
        Should.Throw<ArgumentException>(() => 
            DartThrow.Create(_gameId, _playerId, segment: 25, multiplier: 3, round: 1, dartNumber: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithInvalidDartNumber_ShouldThrow(int invalidDartNumber)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => 
            DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: invalidDartNumber));
    }

    [Fact]
    public void Create_ShouldSetTimestamp()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Timestamp.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var throw1 = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1);
        var throw2 = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 2);

        // Assert
        throw1.Id.ShouldNotBe(throw2.Id);
    }

    [Fact]
    public void Create_WithBustFlag_ShouldSetIsBust()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1, isBust: true);

        // Assert
        dartThrow.IsBust.ShouldBeTrue();
        dartThrow.Points.ShouldBe(0); // Bust = 0 points
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(20, 1, 20)]
    [InlineData(20, 2, 40)]
    [InlineData(20, 3, 60)]
    [InlineData(19, 3, 57)]
    [InlineData(18, 3, 54)]
    [InlineData(25, 1, 25)]
    [InlineData(25, 2, 50)]
    public void Create_AllValidCombinations_ShouldCalculateCorrectPoints(int segment, int multiplier, int expectedPoints)
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment, multiplier, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Points.ShouldBe(expectedPoints);
    }

    [Fact]
    public void Is180Contributor_WithT20_ShouldBeTrue()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 3, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Is180Contributor.ShouldBeTrue();
    }

    [Fact]
    public void Is180Contributor_WithSingle20_ShouldBeFalse()
    {
        // Arrange & Act
        var dartThrow = DartThrow.Create(_gameId, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1);

        // Assert
        dartThrow.Is180Contributor.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullGameId_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            DartThrow.Create(null!, _playerId, segment: 20, multiplier: 1, round: 1, dartNumber: 1));
    }

    [Fact]
    public void Create_WithNullPlayerId_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            DartThrow.Create(_gameId, null!, segment: 20, multiplier: 1, round: 1, dartNumber: 1));
    }
}
