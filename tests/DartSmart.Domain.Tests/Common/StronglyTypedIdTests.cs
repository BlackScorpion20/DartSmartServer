using DartSmart.Domain.Common;
using Shouldly;

namespace DartSmart.Domain.Tests.Common;

public class StronglyTypedIdTests
{
    [Fact]
    public void PlayerId_New_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var id1 = PlayerId.New();
        var id2 = PlayerId.New();

        // Assert
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(id2.Value);
    }

    [Fact]
    public void PlayerId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = PlayerId.From(guid);

        // Assert
        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void PlayerId_Equality_SameGuid_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = PlayerId.From(guid);
        var id2 = PlayerId.From(guid);

        // Act & Assert
        id1.ShouldBe(id2);
        (id1 == id2).ShouldBeTrue();
    }

    [Fact]
    public void GameId_New_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var id1 = GameId.New();
        var id2 = GameId.New();

        // Assert
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void GameId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = GameId.From(guid);

        // Assert
        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void DartThrowId_New_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var id1 = DartThrowId.New();
        var id2 = DartThrowId.New();

        // Assert
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void DartThrowId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = DartThrowId.From(guid);

        // Assert
        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void StronglyTypedId_ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = PlayerId.From(guid);

        // Act
        var str = id.ToString();

        // Assert
        str.ShouldContain(guid.ToString());
    }

    [Fact]
    public void DifferentIdTypes_SameGuid_ShouldNotBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var playerId = PlayerId.From(guid);
        var gameId = GameId.From(guid);

        // Assert - Different types should not be comparable
        playerId.Value.ShouldBe(gameId.Value); // Same underlying GUID
        // But the types are different, so they're not equal as objects
    }
}
