using System;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.DTOs.Bot;

public record SimulateThrowRequest(
    BotDifficulty Difficulty,
    int CurrentScore
);
