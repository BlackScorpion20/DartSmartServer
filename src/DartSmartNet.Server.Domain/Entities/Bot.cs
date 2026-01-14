using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

public class Bot : Entity
{
    public string Name { get; private set; }
    public BotDifficulty Difficulty { get; private set; }
    public decimal AvgPPD { get; private set; }
    public decimal ConsistencyFactor { get; private set; }
    public decimal CheckoutSkill { get; private set; }

    private Bot() : base()
    {
        Name = string.Empty;
    }

    public static Bot Create(string name, BotDifficulty difficulty)
    {
        var (avgPpd, consistency, checkoutSkill) = GetBotParameters(difficulty);

        return new Bot
        {
            Name = name,
            Difficulty = difficulty,
            AvgPPD = avgPpd,
            ConsistencyFactor = consistency,
            CheckoutSkill = checkoutSkill
        };
    }

    private static (decimal avgPpd, decimal consistency, decimal checkoutSkill) GetBotParameters(BotDifficulty difficulty)
    {
        return difficulty switch
        {
            BotDifficulty.Easy => (35m, 0.3m, 0.2m),
            BotDifficulty.Medium => (55m, 0.5m, 0.4m),
            BotDifficulty.Hard => (75m, 0.7m, 0.6m),
            BotDifficulty.Expert => (90m, 0.85m, 0.8m),
            _ => (35m, 0.3m, 0.2m)
        };
    }
}
