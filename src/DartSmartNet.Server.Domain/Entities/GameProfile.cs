using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

/// <summary>
/// Predefined game configurations for quick game setup
/// </summary>
public class GameProfile : Entity
{
    public Guid ProfileId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public GameType GameType { get; private set; }
    public int StartingScore { get; private set; }
    public string OutMode { get; private set; } = "Double"; // Double, Master, Straight
    public string InMode { get; private set; } = "Straight"; // Double, Master, Straight
    public bool IsPublic { get; private set; } // Can be shared with other users
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Extension settings (stored as JSON)
    public string? ExtensionSettings { get; private set; }

    // Navigation
    public User? Owner { get; private set; }

    private GameProfile() { } // EF Core

    public GameProfile(
        Guid ownerId,
        string name,
        GameType gameType,
        int startingScore,
        string outMode = "Double",
        string inMode = "Straight",
        string? description = null,
        bool isPublic = false)
    {
        ProfileId = Guid.NewGuid();
        OwnerId = ownerId;
        Name = name;
        GameType = gameType;
        StartingScore = startingScore;
        OutMode = outMode;
        InMode = inMode;
        Description = description;
        IsPublic = isPublic;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? name = null,
        string? description = null,
        int? startingScore = null,
        string? outMode = null,
        string? inMode = null,
        bool? isPublic = null)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (startingScore.HasValue) StartingScore = startingScore.Value;
        if (outMode != null) OutMode = outMode;
        if (inMode != null) InMode = inMode;
        if (isPublic.HasValue) IsPublic = isPublic.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExtensionSettings(string jsonSettings)
    {
        ExtensionSettings = jsonSettings;
        UpdatedAt = DateTime.UtcNow;
    }
}
