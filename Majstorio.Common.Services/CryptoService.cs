using System.Security.Cryptography;
using System.Text;

namespace Majstorio.Common.Services;

// Salted PBKDF2 password hashing; the salt and hash are stored as Base64 strings.
public class CryptoService : ICryptoService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string GenerateSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));

    public string GenerateHash(string password, string salt) =>
        Convert.ToBase64String(DeriveHash(password, salt));

    public bool VerifyPassword(string password, string hash, string salt)
    {
        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        // Constant-time comparison, so response timing does not reveal how much of the hash matched.
        return CryptographicOperations.FixedTimeEquals(DeriveHash(password, salt), expected);
    }

    private static byte[] DeriveHash(string password, string salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(salt), Iterations, Algorithm, HashSize);
}
