namespace DartSmart.Application.Interfaces;

/// <summary>
/// Password hashing service interface
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
