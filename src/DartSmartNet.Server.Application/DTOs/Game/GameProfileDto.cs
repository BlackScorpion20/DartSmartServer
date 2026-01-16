using System;
namespace DartSmartNet.Server.Application.DTOs.Game;

public record GameProfileDto(
    Guid ProfileId,
    Guid OwnerId,
    string OwnerUsername,
    string Name,
    string? Description,
    string GameType,
    int StartingScore,
    string OutMode,
    string InMode,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
