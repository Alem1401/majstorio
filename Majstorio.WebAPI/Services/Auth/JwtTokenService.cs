using System.Globalization;
using System.Security.Claims;
using System.Text;
using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Majstorio.WebAPI.Services.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        // Configuration is read once here, not on every token.
        _options = options.Value;
        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(ApplicationUser user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(_options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            SigningCredentials = _signingCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtClaimNames.UserId, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtClaimNames.UserName, user.UserName),
                new Claim(JwtClaimNames.Role, RoleNames.For(user.Role)),
                new Claim(JwtClaimNames.TokenVersion, user.TokenVersion.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtClaimNames.TokenId, Guid.NewGuid().ToString())
            ])
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAtUtc);
    }
}
