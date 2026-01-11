using System.Security.Cryptography;
using DartSmart.Application.Interfaces;

namespace DartSmart.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, 
            salt, 
            Iterations, 
            HashAlgorithmName.SHA256, 
            HashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password, 
            salt, 
            Iterations, 
            HashAlgorithmName.SHA256, 
            HashSize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }
}
