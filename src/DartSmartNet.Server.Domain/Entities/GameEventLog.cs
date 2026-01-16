using DartSmartNet.Server.Domain.Common;

namespace DartSmartNet.Server.Domain.Entities;

/// <summary>
/// Stores game events for replay and analysis
/// </summary>
public class GameEventLog : Entity
{
    public Guid EventId { get; private set; }
    public Guid GameId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventData { get; private set; } = string.Empty; // JSON payload
    public string? PlayerUsername { get; private set; }

    // Navigation
    public GameSession? GameSession { get; private set; }

    private GameEventLog() { } // EF Core

    public GameEventLog(
        Guid gameId,
        string eventType,
        string eventData,
        string? playerUsername = null)
    {
        EventId = Guid.NewGuid();
        GameId = gameId;
        Timestamp = DateTime.UtcNow;
        EventType = eventType;
        EventData = eventData;
        PlayerUsername = playerUsername;
    }
}
