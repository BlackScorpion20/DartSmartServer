using Microsoft.Extensions.DependencyInjection;

namespace DartSmart.Application.Common;

/// <summary>
/// Custom mediator implementation (no MediatR dependency)
/// </summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
        where TNotification : INotification;
}

/// <summary>
/// Mediator implementation using IServiceProvider
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType) 
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var method = handlerType.GetMethod("Handle") 
            ?? throw new InvalidOperationException($"Handle method not found on handler");
        
        var result = method.Invoke(handler, new object[] { request, cancellationToken });
        
        if (result is Task<TResponse> task)
            return await task;
        
        throw new InvalidOperationException("Handler did not return expected task type");
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
        where TNotification : INotification
    {
        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
        
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}

/// <summary>
/// Extension methods for registering mediator services
/// </summary>
public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Type[] assemblyMarkerTypes)
    {
        services.AddScoped<IMediator, Mediator>();
        
        var assemblies = assemblyMarkerTypes.Select(t => t.Assembly).Distinct();
        
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))));

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                        (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                         i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)));

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, handlerType);
                }
            }
        }
        
        return services;
    }
}
