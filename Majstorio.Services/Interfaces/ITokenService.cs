using Majstorio.Services.Database;

// Implemented in the WebAPI layer, which owns the JWT signing configuration.
public interface ITokenService
{
    // Creates a signed access token; the caller has already verified the credentials and that the user is active.
    AccessToken CreateAccessToken(ApplicationUser user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
