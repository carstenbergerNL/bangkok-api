using System.Security.Cryptography;
using System.Text;
using Bangkok.Application.Interfaces;

namespace Bangkok.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashIterations = 100000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, saltBytes);
        return (hash, salt);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var computedHash = ComputeHash(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computedHash), Encoding.UTF8.GetBytes(hash));
    }

    private static string ComputeHash(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            passwordBytes,
            salt,
            HashIterations,
            HashAlgorithmName.SHA256,
            32);
        return Convert.ToBase64String(hashBytes);
    }
}
