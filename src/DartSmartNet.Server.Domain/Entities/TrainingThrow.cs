using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Domain.Entities;

public class TrainingThrow : Entity
{
    public Guid TrainingSessionId { get; private set; }
    public int Segment { get; private set; }
    public Multiplier Multiplier { get; private set; }
    public int Points { get; private set; }
    public bool WasSuccessful { get; private set; }
    public DateTime ThrownAt { get; private set; }

    // Navigation property
    public TrainingSession? TrainingSession { get; private set; }

    private TrainingThrow() : base()
    {
        ThrownAt = DateTime.UtcNow;
    }

    public static TrainingThrow Create(Guid trainingSessionId, Score score, bool wasSuccessful)
    {
        return new TrainingThrow
        {
            TrainingSessionId = trainingSessionId,
            Segment = score.Segment,
            Multiplier = score.Multiplier,
            Points = score.Points,
            WasSuccessful = wasSuccessful,
            ThrownAt = DateTime.UtcNow
        };
    }
}
