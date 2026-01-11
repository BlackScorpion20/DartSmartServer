using DartSmart.Application.Commands.Auth;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace DartSmart.Application.Tests.Commands.Auth;

public class RegisterPlayerHandlerTests
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly RegisterPlayerHandler _handler;

    public RegisterPlayerHandlerTests()
    {
        _playerRepository = Substitute.For<IPlayerRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtService = Substitute.For<IJwtService>();
        _handler = new RegisterPlayerHandler(_playerRepository, _passwordHasher, _jwtService);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "test@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashedPassword");
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("accessToken");
        _jwtService.GenerateRefreshToken().Returns("refreshToken");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.AccessToken.ShouldBe("accessToken");
        result.Value.RefreshToken.ShouldBe("refreshToken");
        result.Value.Player.Username.ShouldBe("TestUser");
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "existing@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync("existing@example.com", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Email already registered");
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterPlayerCommand("ExistingUser", "new@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync("ExistingUser", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Username already taken");
    }

    [Fact]
    public async Task Handle_ShouldHashPassword()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "test@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Password123!").Returns("hashedPassword");
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash("Password123!");
    }

    [Fact]
    public async Task Handle_ShouldAddPlayerToRepository()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "test@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashedPassword");
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _playerRepository.Received(1).AddAsync(Arg.Is<Player>(p => 
            p.Username == "TestUser" && 
            p.Email == "test@example.com"), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateJwtTokenWithCorrectClaims()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "test@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashedPassword");
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtService.Received(1).GenerateAccessToken(
            Arg.Any<string>(), 
            "test@example.com", 
            "TestUser");
    }

    [Fact]
    public async Task Handle_SuccessfulRegistration_ShouldReturnPlayerDto()
    {
        // Arrange
        var command = new RegisterPlayerCommand("TestUser", "test@example.com", "Password123!");
        
        _playerRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _playerRepository.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashedPassword");
        _jwtService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("token");
        _jwtService.GenerateRefreshToken().Returns("refresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.Player.ShouldNotBeNull();
        result.Value.Player.Email.ShouldBe("test@example.com");
        result.Value.Player.Statistics.ShouldNotBeNull();
        result.Value.Player.Statistics.TotalGames.ShouldBe(0);
    }
}
