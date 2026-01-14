using DartSmartNet.Server.Application.DTOs.Training;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface ITrainingService
{
    /// <summary>
    /// Starts a new training session
    /// </summary>
    Task<TrainingSessionDto> StartTrainingAsync(Guid userId, GameType trainingType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a training session
    /// </summary>
    Task<TrainingSessionDto> GetTrainingSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a throw in a training session
    /// </summary>
    Task<TrainingSessionDto> RegisterTrainingThrowAsync(Guid sessionId, Score score, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a training session
    /// </summary>
    Task<TrainingSessionDto> EndTrainingAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training history for a user
    /// </summary>
    Task<IEnumerable<TrainingSessionDto>> GetUserTrainingHistoryAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
}
