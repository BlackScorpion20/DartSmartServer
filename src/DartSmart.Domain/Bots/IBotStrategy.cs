using DartSmart.Domain.Entities;
using DartSmart.Domain.Common;

namespace DartSmart.Domain.Bots;

public record BotThrowResult(int Segment, int Multiplier, int DartNumber);

public interface IBotStrategy
{
    /// <summary>
    /// Calculates the throws for a single turn (up to 3 darts).
    /// </summary>
    /// <param name="game">Current state of the game</param>
    /// <param name="botId">The ID of the bot player</param>
    /// <returns>A list of throws. Processing should stop if the game ends.</returns>
    BotThrowResult[] CalculateTurn(Game game, PlayerId botId);
}
