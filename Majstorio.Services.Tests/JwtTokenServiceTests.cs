using System.ComponentModel.DataAnnotations;
using System.Text;
using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Majstorio.Services.Tests;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "Majstorio",
        Audience = "Majstorio.Clients",
        Key = "test-signing-key-that-is-long-enough-for-hs256",
        ExpiryMinutes = 60
    };

    private readonly JwtTokenService _service = new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(Now));

    [Theory]
    [InlineData(UserRole.Customer, RoleNames.Customer)]
    [InlineData(UserRole.Tradesperson, RoleNames.Tradesperson)]
    [InlineData(UserRole.Administrator, RoleNames.Administrator)]
    public void CreateAccessToken_WritesUserIdNameRoleAndTokenVersion(UserRole role, string expectedRole)
    {
        var token = _service.CreateAccessToken(NewUser(role));

        var jwt = new JsonWebToken(token.Token);
        Assert.Equal("42", jwt.GetClaim(JwtClaimNames.UserId).Value);
        Assert.Equal("ana.majstor", jwt.GetClaim(JwtClaimNames.UserName).Value);
        Assert.Equal(expectedRole, jwt.GetClaim(JwtClaimNames.Role).Value);
        Assert.Equal("3", jwt.GetClaim(JwtClaimNames.TokenVersion).Value);
        Assert.True(Guid.TryParse(jwt.GetClaim(JwtClaimNames.TokenId).Value, out _));
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
    }

    [Fact]
    public void CreateAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var token = _service.CreateAccessToken(NewUser());

        var jwt = new JsonWebToken(token.Token);
        Assert.Equal(Now.UtcDateTime.AddMinutes(Options.ExpiryMinutes), token.ExpiresAtUtc);
        Assert.Equal(token.ExpiresAtUtc, jwt.ValidTo);
        Assert.Equal(Now.UtcDateTime, jwt.IssuedAt);
    }

    [Fact]
    public void CreateAccessToken_GivesEachTokenAUniqueId()
    {
        var first = new JsonWebToken(_service.CreateAccessToken(NewUser()).Token);
        var second = new JsonWebToken(_service.CreateAccessToken(NewUser()).Token);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateAccessToken_ValidatesWithTheConfiguredKeyIssuerAndAudience()
    {
        var token = _service.CreateAccessToken(NewUser());

        var result = await ValidateAsync(token.Token, Options.Key, Options.Issuer, Options.Audience);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Theory]
    [InlineData("a-different-signing-key-that-is-long-enough", "Majstorio", "Majstorio.Clients")]
    [InlineData("test-signing-key-that-is-long-enough-for-hs256", "SomeoneElse", "Majstorio.Clients")]
    [InlineData("test-signing-key-that-is-long-enough-for-hs256", "Majstorio", "SomeoneElse")]
    public async Task CreateAccessToken_IsRejectedWithWrongKeyIssuerOrAudience(string key, string issuer, string audience)
    {
        var token = _service.CreateAccessToken(NewUser());

        var result = await ValidateAsync(token.Token, key, issuer, audience);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("", "Majstorio.Clients", "test-signing-key-that-is-long-enough-for-hs256", 60, nameof(JwtOptions.Issuer))]
    [InlineData("Majstorio", "", "test-signing-key-that-is-long-enough-for-hs256", 60, nameof(JwtOptions.Audience))]
    [InlineData("Majstorio", "Majstorio.Clients", "too-short", 60, nameof(JwtOptions.Key))]
    [InlineData("Majstorio", "Majstorio.Clients", "test-signing-key-that-is-long-enough-for-hs256", 0, nameof(JwtOptions.ExpiryMinutes))]
    public void Options_RejectMissingOrUnsafeValues(string issuer, string audience, string key, int expiryMinutes, string invalidMember)
    {
        var options = new JwtOptions { Issuer = issuer, Audience = audience, Key = key, ExpiryMinutes = expiryMinutes };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), errors, validateAllProperties: true));
        Assert.Contains(errors, error => error.MemberNames.Contains(invalidMember));
    }

    [Fact]
    public void Options_AcceptTheExampleShape()
    {
        var errors = new List<ValidationResult>();

        Assert.True(Validator.TryValidateObject(Options, new ValidationContext(Options), errors, validateAllProperties: true));
    }

    private static Task<TokenValidationResult> ValidateAsync(string token, string key, string issuer, string audience) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            // The token is issued at a fixed past time; lifetime is checked separately above.
            ValidateLifetime = false
        });

    private static ApplicationUser NewUser(UserRole role = UserRole.Customer) => new()
    {
        Id = 42,
        UserName = "ana.majstor",
        PasswordHash = "test-hash",
        PasswordSalt = "test-salt",
        Role = role,
        TokenVersion = 3
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
