using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public interface IBotService
{
    Task<Score> SimulateThrowAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default);
    Task<IEnumerable<Score>> SimulateRoundAsync(BotDifficulty difficulty, int currentScore, CancellationToken cancellationToken = default);
}
