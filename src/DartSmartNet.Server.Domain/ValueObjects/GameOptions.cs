namespace DartSmartNet.Server.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

[Owned]
public sealed record GameOptions(
    string InMode,
    string OutMode,
    string CricketMode = "Standard"
)
{
    public static GameOptions DefaultX01() => new("Straight", "Double");

    public static GameOptions DefaultCricket() => new("Straight", "Straight", "Standard");
}
