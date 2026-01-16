using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DartSmartNet.Server.Application.Services;

public class GameProfileService : IGameProfileService
{
    private readonly IGameProfileRepository _profileRepository;
    private readonly IGameService _gameService;
    private readonly ILogger<GameProfileService> _logger;

    public GameProfileService(
        IGameProfileRepository profileRepository,
        IGameService gameService,
        ILogger<GameProfileService> logger)
    {
        _profileRepository = profileRepository;
        _gameService = gameService;
        _logger = logger;
    }

    public async Task<IEnumerable<GameProfileDto>> GetUserProfilesAsync(Guid userId)
    {
        var profiles = await _profileRepository.GetByUserIdAsync(userId);
        return profiles.Select(MapToDto);
    }

    public async Task<IEnumerable<GameProfileDto>> GetPublicProfilesAsync(int limit = 50)
    {
        var profiles = await _profileRepository.GetPublicProfilesAsync(limit);
        return profiles.Select(MapToDto);
    }

    public async Task<GameProfileDto?> GetProfileAsync(Guid profileId)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        return profile != null ? MapToDto(profile) : null;
    }

    public async Task<GameProfileDto> CreateProfileAsync(Guid userId, CreateGameProfileRequest request)
    {
        var profile = new GameProfile(
            userId,
            request.Name,
            Enum.Parse<GameType>(request.GameType),
            request.StartingScore,
            request.OutMode,
            request.InMode,
            request.Description,
            request.IsPublic
        );

        await _profileRepository.AddAsync(profile);
        return MapToDto(profile);
    }

    public async Task<GameProfileDto?> UpdateProfileAsync(Guid userId, Guid profileId, UpdateGameProfileRequest request)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null || profile.OwnerId != userId)
        {
            return null;
        }

        profile.Update(
            request.Name,
            request.Description,
            request.StartingScore,
            request.OutMode,
            request.InMode,
            request.IsPublic
        );

        await _profileRepository.UpdateAsync(profile);
        return MapToDto(profile);
    }

    public async Task<bool> DeleteProfileAsync(Guid userId, Guid profileId)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null || profile.OwnerId != userId)
        {
            return false;
        }

        await _profileRepository.DeleteAsync(profile);
        return true;
    }

    public async Task<GameStateDto?> CreateGameFromProfileAsync(Guid userId, Guid profileId, Guid[] playerIds, bool isOnline)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null)
        {
            return null;
        }

        var options = new DartSmartNet.Server.Domain.ValueObjects.GameOptions(profile.InMode, profile.OutMode, "Standard");

        return await _gameService.CreateGameAsync(
            profile.GameType,
            profile.StartingScore,
            playerIds,
            isOnline,
            options
        );
    }

    private static GameProfileDto MapToDto(GameProfile profile)
    {
        return new GameProfileDto(
            profile.ProfileId,
            profile.OwnerId,
            profile.Owner?.Username ?? "Unknown",
            profile.Name,
            profile.Description,
            profile.GameType.ToString(),
            profile.StartingScore,
            profile.OutMode,
            profile.InMode,
            profile.IsPublic,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }
}
