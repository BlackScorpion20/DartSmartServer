using DartSmart.Application.Common;
using Shouldly;

namespace DartSmart.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_ShouldReturnFailureResult()
    {
        // Arrange & Act
        var result = Result<int>.Failure("Something went wrong");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Something went wrong");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldContainAllErrors()
    {
        // Arrange & Act
        var result = Result<int>.Failure(new[] { "Error 1", "Error 2", "Error 3" });

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.Errors.ShouldContain("Error 3");
    }

    [Fact]
    public void Match_OnSuccess_ShouldInvokeSuccessFunction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var successInvoked = false;
        var failureInvoked = false;

        // Act
        var output = result.Match(
            value => { successInvoked = true; return value * 2; },
            error => { failureInvoked = true; return -1; }
        );

        // Assert
        successInvoked.ShouldBeTrue();
        failureInvoked.ShouldBeFalse();
        output.ShouldBe(84);
    }

    [Fact]
    public void Match_OnFailure_ShouldInvokeFailureFunction()
    {
        // Arrange
        var result = Result<int>.Failure("Error");
        var successInvoked = false;
        var failureInvoked = false;

        // Act
        var output = result.Match(
            value => { successInvoked = true; return value * 2; },
            error => { failureInvoked = true; return -1; }
        );

        // Assert
        successInvoked.ShouldBeFalse();
        failureInvoked.ShouldBeTrue();
        output.ShouldBe(-1);
    }

    [Fact]
    public void NonGenericResult_Success_ShouldBeSuccess()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void NonGenericResult_Failure_ShouldHaveError()
    {
        // Arrange & Act
        var result = Result.Failure("Error occurred");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Error occurred");
    }

    [Fact]
    public void Success_WithComplexType_ShouldContainValue()
    {
        // Arrange
        var dto = new TestDto { Name = "Test", Value = 123 };

        // Act
        var result = Result<TestDto>.Success(dto);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Test");
        result.Value.Value.ShouldBe(123);
    }

    [Fact]
    public void Errors_OnSuccess_ShouldBeEmpty()
    {
        // Arrange & Act
        var result = Result<int>.Success(1);

        // Assert
        result.Errors.ShouldBeEmpty();
    }

    private class TestDto
    {
        public string Name { get; init; } = string.Empty;
        public int Value { get; init; }
    }
}
