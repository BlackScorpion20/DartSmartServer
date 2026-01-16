using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Game;

namespace DartSmartNet.Server.Application.Services;

public interface IGameProfileService
{
    Task<IEnumerable<GameProfileDto>> GetUserProfilesAsync(Guid userId);
    Task<IEnumerable<GameProfileDto>> GetPublicProfilesAsync(int limit = 50);
    Task<GameProfileDto?> GetProfileAsync(Guid profileId);
    Task<GameProfileDto> CreateProfileAsync(Guid userId, CreateGameProfileRequest request);
    Task<GameProfileDto?> UpdateProfileAsync(Guid userId, Guid profileId, UpdateGameProfileRequest request);
    Task<bool> DeleteProfileAsync(Guid userId, Guid profileId);
    Task<GameStateDto?> CreateGameFromProfileAsync(Guid userId, Guid profileId, Guid[] playerIds, bool isOnline);
}
