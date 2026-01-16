using System;
namespace DartSmartNet.Server.Application.DTOs.Game;

public record CreateGameProfileRequest(
    string Name,
    string? Description,
    string GameType,
    int StartingScore,
    string OutMode,
    string InMode,
    bool IsPublic
);

public record UpdateGameProfileRequest(
    string? Name,
    string? Description,
    int? StartingScore,
    string? OutMode,
    string? InMode,
    bool? IsPublic
);

public record CreateGameFromProfileRequest(
    Guid[] PlayerIds,
    bool IsOnline = false
);
