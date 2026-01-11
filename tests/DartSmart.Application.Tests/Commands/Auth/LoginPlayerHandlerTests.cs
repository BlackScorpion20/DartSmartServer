using DartSmart.Application.Commands.Auth;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace DartSmart.Application.Tests.Commands.Auth;

public class LoginPlayerHandlerTests
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly LoginPlayerHandler _handler;

    public LoginPlayerHandlerTests()
    {
        _playerRepository = Substitute.For<IPlayerRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtService = Substitute.For<IJwtService>();
        _handler = new LoginPlayerHandler(_playerRepository, _passwordHasher, _jwtService);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "hashedPassword");
        var command = new LoginPlayerCommand("test@example.com", "Password123!");
        
        _playerRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(player);
        _passwordHasher.Verify("Password123!", "hashedPassword").Returns(true);
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("accessToken");
        _jwtService.GenerateRefreshToken().Returns("refreshToken");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.AccessToken.ShouldBe("accessToken");
        result.Value.RefreshToken.ShouldBe("refreshToken");
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginPlayerCommand("nonexistent@example.com", "Password123!");
        
        _playerRepository.GetByEmailAsync("nonexistent@example.com", Arg.Any<CancellationToken>()).Returns((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "hashedPassword");
        var command = new LoginPlayerCommand("test@example.com", "WrongPassword!");
        
        _playerRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(player);
        _passwordHasher.Verify("WrongPassword!", "hashedPassword").Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_ShouldVerifyPasswordWithHasher()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "storedHash");
        var command = new LoginPlayerCommand("test@example.com", "InputPassword");
        
        _playerRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(player);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Verify("InputPassword", "storedHash");
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ShouldReturnPlayerData()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "hashedPassword");
        var command = new LoginPlayerCommand("test@example.com", "Password123!");
        
        _playerRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(player);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.Player.Username.ShouldBe("TestUser");
        result.Value.Player.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task Handle_ShouldGenerateRefreshToken()
    {
        // Arrange
        var player = Player.Create("TestUser", "test@example.com", "hashedPassword");
        var command = new LoginPlayerCommand("test@example.com", "Password123!");
        
        _playerRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(player);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("uniqueRefresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtService.Received(1).GenerateRefreshToken();
        result.Value!.RefreshToken.ShouldBe("uniqueRefresh");
    }
}
