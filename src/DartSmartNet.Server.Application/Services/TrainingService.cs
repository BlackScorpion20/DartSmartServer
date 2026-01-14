using DartSmartNet.Server.Application.DTOs.Training;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public class TrainingService : ITrainingService
{
    private readonly ITrainingRepository _trainingRepository;

    public TrainingService(ITrainingRepository trainingRepository)
    {
        _trainingRepository = trainingRepository;
    }

    public async Task<TrainingSessionDto> StartTrainingAsync(
        Guid userId,
        GameType trainingType,
        CancellationToken cancellationToken = default)
    {
        var session = TrainingSession.Create(userId, trainingType);
        await _trainingRepository.AddAsync(session, cancellationToken);

        return MapToDto(session);
    }

    public async Task<TrainingSessionDto> GetTrainingSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _trainingRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException($"Training session {sessionId} not found");
        }

        return MapToDto(session);
    }

    public async Task<TrainingSessionDto> RegisterTrainingThrowAsync(
        Guid sessionId,
        Score score,
        CancellationToken cancellationToken = default)
    {
        var session = await _trainingRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException($"Training session {sessionId} not found");
        }

        if (session.Status != TrainingStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot register throw in {session.Status} session");
        }

        // Determine if the throw was successful based on training type
        var wasSuccessful = IsThrowSuccessful(session, score);

        session.RegisterThrow(score, wasSuccessful);
        await _trainingRepository.UpdateAsync(session, cancellationToken);

        return MapToDto(session);
    }

    public async Task<TrainingSessionDto> EndTrainingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _trainingRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException($"Training session {sessionId} not found");
        }

        if (session.Status == TrainingStatus.InProgress)
        {
            session.Abandon();
            await _trainingRepository.UpdateAsync(session, cancellationToken);
        }

        return MapToDto(session);
    }

    public async Task<IEnumerable<TrainingSessionDto>> GetUserTrainingHistoryAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _trainingRepository.GetUserTrainingHistoryAsync(userId, limit, cancellationToken);

        return sessions.Select(MapToDto);
    }

    private bool IsThrowSuccessful(TrainingSession session, Score score)
    {
        return session.TrainingType switch
        {
            // Round the Board: Hit the single of the current target number
            GameType.TrainingRoundTheBoard =>
                score.Segment == session.CurrentTarget && score.Multiplier == Multiplier.Single,

            // Bob's 27 Doubles: Hit the double of the current target number
            GameType.TrainingBobsDoubles =>
                score.Segment == session.CurrentTarget && score.Multiplier == Multiplier.Double,

            // Bob's 27 Triples: Hit the triple of the current target number
            GameType.TrainingBobsTriples =>
                score.Segment == session.CurrentTarget && score.Multiplier == Multiplier.Triple,

            // Bullseye Training: Hit either single bull or double bull
            GameType.TrainingBullseye =>
                score.Segment == 25,

            // Segment Focus: Hit the target segment (any multiplier counts)
            GameType.TrainingSegmentFocus =>
                score.Segment == session.CurrentTarget,

            // Checkout Practice: Hit a valid checkout double
            GameType.TrainingCheckoutPractice =>
                score.Multiplier == Multiplier.Double && score.Segment > 0,

            // Shanghai Drill: Hit the current target in sequence (Single -> Double -> Triple)
            GameType.TrainingShanghaiDrill =>
                IsValidShanghaiThrow(session, score),

            _ => false
        };
    }

    private bool IsValidShanghaiThrow(TrainingSession session, Score score)
    {
        // Shanghai requires hitting S-D-T in sequence for each number
        if (score.Segment != session.CurrentTarget)
            return false;

        var throwsOnCurrentTarget = session.Throws
            .Count(t => t.WasSuccessful && t.Segment == session.CurrentTarget);

        return throwsOnCurrentTarget switch
        {
            0 => score.Multiplier == Multiplier.Single,
            1 => score.Multiplier == Multiplier.Double,
            2 => score.Multiplier == Multiplier.Triple,
            _ => false
        };
    }

    private TrainingSessionDto MapToDto(TrainingSession session)
    {
        var throws = session.Throws.Select(t => new TrainingThrowDto(
            t.Segment,
            t.Multiplier.ToString(),
            t.Points,
            t.WasSuccessful,
            t.ThrownAt
        )).ToList();

        return new TrainingSessionDto(
            session.Id,
            session.UserId,
            session.TrainingType,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.CurrentTarget,
            session.Score,
            session.DartsThrown,
            session.SuccessfulHits,
            session.AccuracyPercentage,
            throws
        );
    }
}
