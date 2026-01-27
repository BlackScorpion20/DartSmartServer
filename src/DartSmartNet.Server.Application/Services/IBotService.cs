using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface IBotService
{
    /// <summary>
    /// Simulates a single X01 throw aiming for high scores or checkout
    /// </summary>
    Task<Score> SimulateThrowAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simulates a full round (up to 3 darts) for X01
    /// </summary>
    Task<IEnumerable<Score>> SimulateRoundAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simulates a throw for Around The Clock - aiming for a specific target segment
    /// </summary>
    Task<Score> SimulateAtcThrowAsync(BotDifficulty difficulty, int targetSegment, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simulates a throw for JDC Challenge - aiming for specific target with Shanghai or doubles-only mode
    /// </summary>
    Task<Score> SimulateJdcThrowAsync(BotDifficulty difficulty, int targetSegment, bool doublesOnly, CancellationToken cancellationToken = default);
}
