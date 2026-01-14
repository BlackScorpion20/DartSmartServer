using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Domain.Events;

public sealed record ThrowRegisteredEvent(
    Guid GameId,
    Guid UserId,
    DartThrow Throw,
    DateTime RegisteredAt
);
