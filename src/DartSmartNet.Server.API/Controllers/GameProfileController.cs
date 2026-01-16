using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartSmartNet.Server.API.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize]
public class GameProfileController : ControllerBase
{
    private readonly IGameProfileService _profileService;
    private readonly ILogger<GameProfileController> _logger;

    public GameProfileController(
        IGameProfileService profileService,
        ILogger<GameProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get all profiles for the current user
    /// </summary>
    [HttpGet("my-profiles")]
    public async Task<ActionResult<IEnumerable<GameProfileDto>>> GetMyProfiles()
    {
        var userId = GetUserId();
        var profiles = await _profileService.GetUserProfilesAsync(userId);
        return Ok(profiles);
    }

    /// <summary>
    /// Get public profiles (shared by other users)
    /// </summary>
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<GameProfileDto>>> GetPublicProfiles(
        [FromQuery] int limit = 50)
    {
        var profiles = await _profileService.GetPublicProfilesAsync(limit);
        return Ok(profiles);
    }

    /// <summary>
    /// Get a specific profile by ID
    /// </summary>
    [HttpGet("{profileId:guid}")]
    public async Task<ActionResult<GameProfileDto>> GetProfile(Guid profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId);
        if (profile == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Create a new game profile
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GameProfileDto>> CreateProfile(
        [FromBody] CreateGameProfileRequest request)
    {
        var userId = GetUserId();
        var profile = await _profileService.CreateProfileAsync(userId, request);
        return CreatedAtAction(
            nameof(GetProfile),
            new { profileId = profile.ProfileId },
            profile);
    }

    /// <summary>
    /// Update an existing profile
    /// </summary>
    [HttpPut("{profileId:guid}")]
    public async Task<ActionResult<GameProfileDto>> UpdateProfile(
        Guid profileId,
        [FromBody] UpdateGameProfileRequest request)
    {
        var userId = GetUserId();
        var profile = await _profileService.UpdateProfileAsync(userId, profileId, request);

        if (profile == null)
        {
            return NotFound(new { message = "Profile not found or access denied" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Delete a profile
    /// </summary>
    [HttpDelete("{profileId:guid}")]
    public async Task<ActionResult> DeleteProfile(Guid profileId)
    {
        var userId = GetUserId();
        var success = await _profileService.DeleteProfileAsync(userId, profileId);

        if (!success)
        {
            return NotFound(new { message = "Profile not found or access denied" });
        }

        return NoContent();
    }

    /// <summary>
    /// Create a game from a profile
    /// </summary>
    [HttpPost("{profileId:guid}/create-game")]
    public async Task<ActionResult<GameStateDto>> CreateGameFromProfile(
        Guid profileId,
        [FromBody] CreateGameFromProfileRequest request)
    {
        var userId = GetUserId();
        var gameState = await _profileService.CreateGameFromProfileAsync(
            userId, profileId, request.PlayerIds, request.IsOnline);

        if (gameState == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        return CreatedAtAction(
            nameof(GameController.GetGame),
            "Game",
            new { gameId = gameState.GameId },
            gameState);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        return userId;
    }
}
