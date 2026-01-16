using System;
using System.Collections.Generic;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.DTOs.Training;

public sealed record TrainingSessionDto(
    Guid SessionId,
    Guid UserId,
    GameType TrainingType,
    TrainingStatus Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    int CurrentTarget,
    int Score,
    int DartsThrown,
    int SuccessfulHits,
    decimal AccuracyPercentage,
    List<TrainingThrowDto> Throws
);

public sealed record TrainingThrowDto(
    int Segment,
    string Multiplier,
    int Points,
    bool WasSuccessful,
    DateTime ThrownAt
);
