namespace DartSmartNet.Server.Domain.Events;

/// <summary>
/// Common data structure for game state details in events
/// </summary>
public record GameEventData(
    string Mode,
    int? PointsLeft = null,
    int? DartNumber = null,
    int? Segment = null,
    int? Multiplier = null,
    int? Points = null,
    int? DartsThrown = null,
    int? TotalPoints = null,
    double? AveragePPD = null,
    int? PointsBefore = null,
    int? PointsThrown = null,
    int? PlayerOrder = null
);
