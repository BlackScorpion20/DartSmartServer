using DartSmart.Domain.Common;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Domain.Entities;

/// <summary>
/// Player aggregate root
/// </summary>
public class Player : Entity<PlayerId>, IAggregateRoot
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public PlayerStatistics Statistics { get; private set; } = PlayerStatistics.Empty;

    private Player() { } // EF Core constructor

    public static Player Create(string username, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Player
        {
            Id = PlayerId.New(),
            Username = username.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            Statistics = PlayerStatistics.Empty
        };
    }

    public void UpdateStatistics(PlayerStatistics newStats)
    {
        Statistics = newStats ?? throw new ArgumentNullException(nameof(newStats));
    }

    public void RecordGameResult(bool isWin, int dartsThrown, int pointsScored, int? checkoutScore = null)
    {
        Statistics = Statistics.WithGame(isWin, dartsThrown, pointsScored, checkoutScore);
    }

    public void Record180()
    {
        Statistics = Statistics.With180();
    }

    public void ChangeUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be empty", nameof(newUsername));
        
        Username = newUsername.Trim();
    }

    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));
        
        PasswordHash = newPasswordHash;
    }
}
