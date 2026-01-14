namespace DartSmartNet.Server.Domain.Events;

public sealed record GameStartedEvent(
    Guid GameId,
    Guid[] PlayerIds,
    DateTime StartedAt
);
