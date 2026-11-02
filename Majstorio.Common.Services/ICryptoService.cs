namespace Majstorio.Common.Services;

public interface ICryptoService
{
    string GenerateSalt();

    string GenerateHash(string password, string salt);

    bool VerifyPassword(string password, string hash, string salt);
}
