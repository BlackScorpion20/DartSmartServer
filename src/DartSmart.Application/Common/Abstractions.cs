namespace DartSmart.Application.Common;

/// <summary>
/// Marker interface for requests that return a response
/// </summary>
public interface IRequest<TResponse> { }

/// <summary>
/// Marker interface for requests without a response (commands)
/// </summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>
/// Unit type for commands without return value
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();
    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}

/// <summary>
/// Handler for requests
/// </summary>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for commands without response
/// </summary>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit>
{
}

/// <summary>
/// Notification (domain event) interface
/// </summary>
public interface INotification { }

/// <summary>
/// Handler for notifications
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}
