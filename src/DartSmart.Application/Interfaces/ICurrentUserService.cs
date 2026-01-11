namespace DartSmart.Application.Interfaces;

/// <summary>
/// Current user context
/// </summary>
public interface ICurrentUserService
{
    string? PlayerId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
