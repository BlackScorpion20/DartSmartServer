namespace DartSmartNet.Server.Domain.Events;

public sealed record GameEndedEvent(
    Guid GameId,
    Guid WinnerId,
    DateTime EndedAt
);
