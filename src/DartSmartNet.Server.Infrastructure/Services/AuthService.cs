using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using DartSmartNet.Server.Application.DTOs.Auth;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace DartSmartNet.Server.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IStatsRepository _statsRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IStatsRepository statsRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _statsRepository = statsRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Validate username and email are unique
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Validate password strength
        if (request.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters");
        }

        // Create user
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Username, request.Email, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        // Create initial stats for user
        var stats = PlayerStats.CreateForUser(user.Id);
        await _statsRepository.AddAsync(stats, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            accessToken,
            refreshToken,
            _jwtSettings.ExpirationMinutes * 60
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Find user
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new InvalidOperationException("Account is deactivated");
        }

        // Update last login
        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            accessToken,
            refreshToken,
            _jwtSettings.ExpirationMinutes * 60
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Validate refresh token
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new InvalidOperationException("Invalid or expired refresh token");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User not found or inactive");
        }

        // Revoke old refresh token
        await _refreshTokenRepository.RevokeAsync(refreshToken, cancellationToken);

        // Generate new tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            accessToken,
            newRefreshToken,
            _jwtSettings.ExpirationMinutes * 60
        );
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.ValidateToken(token);

        if (principal == null)
        {
            return false;
        }

        // Extract user ID from claims
        var userIdClaim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return false;
        }

        // Verify user still exists and is active
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive;
    }
}
