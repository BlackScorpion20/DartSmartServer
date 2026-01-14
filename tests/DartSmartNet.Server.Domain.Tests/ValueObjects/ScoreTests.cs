using DartSmartNet.Server.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace DartSmartNet.Server.Domain.Tests.ValueObjects;

public class ScoreTests
{
    [Theory]
    [InlineData(20, 20)]
    [InlineData(1, 1)]
    [InlineData(19, 19)]
    public void Single_ShouldCalculateCorrectPoints(int segment, int expectedPoints)
    {
        // Act
        var score = Score.Single(segment);

        // Assert
        score.Segment.ShouldBe(segment);
        score.Multiplier.ShouldBe(Multiplier.Single);
        score.Points.ShouldBe(expectedPoints);
    }

    [Theory]
    [InlineData(20, 40)]
    [InlineData(16, 32)]
    [InlineData(1, 2)]
    public void Double_ShouldCalculateCorrectPoints(int segment, int expectedPoints)
    {
        // Act
        var score = Score.Double(segment);

        // Assert
        score.Segment.ShouldBe(segment);
        score.Multiplier.ShouldBe(Multiplier.Double);
        score.Points.ShouldBe(expectedPoints);
        score.IsDouble().ShouldBeTrue();
    }

    [Theory]
    [InlineData(20, 60)]
    [InlineData(19, 57)]
    [InlineData(1, 3)]
    public void Triple_ShouldCalculateCorrectPoints(int segment, int expectedPoints)
    {
        // Act
        var score = Score.Triple(segment);

        // Assert
        score.Segment.ShouldBe(segment);
        score.Multiplier.ShouldBe(Multiplier.Triple);
        score.Points.ShouldBe(expectedPoints);
        score.IsTriple().ShouldBeTrue();
    }

    [Fact]
    public void Miss_ShouldHaveZeroPoints()
    {
        // Act
        var score = Score.Miss();

        // Assert
        score.Segment.ShouldBe(0);
        score.Points.ShouldBe(0);
        score.ToString().ShouldBe("MISS");
    }

    [Fact]
    public void SingleBull_ShouldBe25Points()
    {
        // Act
        var score = Score.SingleBull();

        // Assert
        score.Segment.ShouldBe(25);
        score.Points.ShouldBe(25);
        score.IsBullseye().ShouldBeTrue();
    }

    [Fact]
    public void DoubleBull_ShouldBe50Points()
    {
        // Act
        var score = Score.DoubleBull();

        // Assert
        score.Segment.ShouldBe(25);
        score.Multiplier.ShouldBe(Multiplier.Double);
        score.Points.ShouldBe(50);
        score.IsBullseye().ShouldBeTrue();
        score.IsDouble().ShouldBeTrue();
    }

    [Theory]
    [InlineData(20, Multiplier.Triple, "T20")]
    [InlineData(20, Multiplier.Double, "D20")]
    [InlineData(20, Multiplier.Single, "20")]
    [InlineData(1, Multiplier.Single, "1")]
    public void ToString_ShouldFormatCorrectly(int segment, Multiplier multiplier, string expected)
    {
        // Act
        var score = multiplier switch
        {
            Multiplier.Single => Score.Single(segment),
            Multiplier.Double => Score.Double(segment),
            Multiplier.Triple => Score.Triple(segment),
            _ => Score.Miss()
        };

        // Assert
        score.ToString().ShouldBe(expected);
    }

    [Fact]
    public void SingleBull_ToString_ShouldReturn25()
    {
        // Act
        var score = Score.SingleBull();

        // Assert
        score.ToString().ShouldBe("25");
    }

    [Fact]
    public void DoubleBull_ToString_ShouldReturnBULL()
    {
        // Act
        var score = Score.DoubleBull();

        // Assert
        score.ToString().ShouldBe("BULL");
    }
}
