using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Auth;
using DartSmartNet.Server.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registration attempt for username: {Username}", request.Username);

            var response = await _authService.RegisterAsync(request, cancellationToken);

            _logger.LogInformation("User {Username} registered successfully", request.Username);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for username {Username}: {Message}", request.Username, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration failed",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for username: {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);

            var response = await _authService.LoginAsync(request, cancellationToken);

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Login failed for username {Username}: {Message}", request.Username, ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Login failed",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for username: {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred during login"
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", response.UserId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Token refresh failed",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred during token refresh"
            });
        }
    }

    /// <summary>
    /// Validate the current access token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken(CancellationToken cancellationToken)
    {
        try
        {
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Token validation failed",
                    Detail = "No token provided"
                });
            }

            var isValid = await _authService.ValidateTokenAsync(token, cancellationToken);

            if (!isValid)
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Token validation failed",
                    Detail = "Token is invalid or expired"
                });
            }

            return Ok(new TokenValidationResponse { IsValid = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = "An unexpected error occurred during token validation"
            });
        }
    }
}

public record RefreshTokenRequest(string RefreshToken);
public record TokenValidationResponse { public bool IsValid { get; init; } }
