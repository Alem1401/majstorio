using Microsoft.IdentityModel.JsonWebTokens;

namespace Majstorio.WebAPI.Services.Auth;

// Claim names written into the token; token validation and the current-user accessor read the same names.
public static class JwtClaimNames
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string UserName = JwtRegisteredClaimNames.UniqueName;
    public const string Role = "role";
    public const string TokenId = JwtRegisteredClaimNames.Jti;
    // Compared with ApplicationUser.TokenVersion so logout can invalidate issued tokens.
    public const string TokenVersion = "token_version";
}
