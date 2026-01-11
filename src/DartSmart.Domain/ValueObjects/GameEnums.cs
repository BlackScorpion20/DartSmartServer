namespace DartSmart.Domain.ValueObjects;

/// <summary>
/// Game type enumeration
/// </summary>
public enum GameType
{
    X01_301 = 301,
    X01_501 = 501,
    X01_701 = 701,
    Cricket = 1000,
    Shanghai = 1001,
    AroundTheClock = 1002
}

/// <summary>
/// X01 In mode (how to start scoring)
/// </summary>
public enum X01InMode
{
    StraightIn = 0,
    DoubleIn = 1,
    MasterIn = 2  // Double or Triple
}

/// <summary>
/// X01 Out mode (how to finish the game)
/// </summary>
public enum X01OutMode
{
    StraightOut = 0,
    DoubleOut = 1,
    MasterOut = 2  // Double or Triple
}

/// <summary>
/// Game status
/// </summary>
public enum GameStatus
{
    Lobby = 0,
    InProgress = 1,
    Finished = 2
}
