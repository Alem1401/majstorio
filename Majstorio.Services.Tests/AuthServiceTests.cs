using Majstorio.Common.Services;
using Majstorio.Model.Requests.Auth;
using Majstorio.Model.Results;
using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.Auth;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Majstorio.Services.Tests;

public class AuthServiceTests : IDisposable
{
    private const string Password = "Correct-Horse-1";

    private readonly MajstorioDbContext _ctx = new(new DbContextOptionsBuilder<MajstorioDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly CryptoService _crypto = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var tokenService = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "Majstorio",
            Audience = "Majstorio.Clients",
            Key = "test-signing-key-that-is-long-enough-for-hs256",
            ExpiryMinutes = 60
        }), TimeProvider.System);
        _service = new AuthService(_ctx, _crypto, tokenService);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenForThatUser()
    {
        var user = await AddUserAsync("ana", role: UserRole.Tradesperson, tokenVersion: 2);

        var result = await _service.LoginAsync(new() { UserName = "ana", Password = Password });

        Assert.True(result.IsSuccess);
        Assert.Equal(RoleNames.Tradesperson, result.Value!.Role);
        Assert.True(result.Value.ExpiresAtUtc > DateTime.UtcNow);
        var jwt = new JsonWebToken(result.Value.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.GetClaim(JwtClaimNames.UserId).Value);
        Assert.Equal(RoleNames.Tradesperson, jwt.GetClaim(JwtClaimNames.Role).Value);
        Assert.Equal("2", jwt.GetClaim(JwtClaimNames.TokenVersion).Value);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownUser_ReturnTheSameUnauthorizedMessage()
    {
        await AddUserAsync("ana");

        var wrongPassword = await _service.LoginAsync(new() { UserName = "ana", Password = "Wrong-Password-1" });
        var unknownUser = await _service.LoginAsync(new() { UserName = "nobody", Password = Password });

        Assert.Equal(ErrorType.Unauthorized, wrongPassword.ErrorType);
        Assert.Equal(ErrorType.Unauthorized, unknownUser.ErrorType);
        Assert.Equal(wrongPassword.Message, unknownUser.Message);
        Assert.Null(wrongPassword.Value);
    }

    [Fact]
    public async Task Login_InactiveUserWithCorrectPassword_IsForbidden()
    {
        await AddUserAsync("ana", isActive: false);

        var result = await _service.LoginAsync(new() { UserName = "ana", Password = Password });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Login_InactiveUserWithWrongPassword_DoesNotRevealTheAccountState()
    {
        await AddUserAsync("ana", isActive: false);

        var result = await _service.LoginAsync(new() { UserName = "ana", Password = "Wrong-Password-1" });

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Logout_IncrementsTokenVersionSoEarlierTokensAreOutdated()
    {
        var user = await AddUserAsync("ana", tokenVersion: 5);
        Assert.True(await _service.IsTokenVersionCurrentAsync(user.Id, 5));

        Assert.True((await _service.LogoutAsync(user.Id)).IsSuccess);

        Assert.False(await _service.IsTokenVersionCurrentAsync(user.Id, 5));
        Assert.True(await _service.IsTokenVersionCurrentAsync(user.Id, 6));
    }

    [Fact]
    public async Task Logout_UnknownUser_ReturnsNotFound()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.LogoutAsync(999)).ErrorType);
    }

    [Fact]
    public async Task IsTokenVersionCurrent_IsFalseForDeactivatedOrMissingUsers()
    {
        var user = await AddUserAsync("ana", isActive: false);

        Assert.False(await _service.IsTokenVersionCurrentAsync(user.Id, user.TokenVersion));
        Assert.False(await _service.IsTokenVersionCurrentAsync(999, 0));
    }

    [Theory]
    [InlineData("", "secret", false)]
    [InlineData("ana", "", false)]
    [InlineData("ana", "secret", true)]
    public void LoginValidator_RequiresUserNameAndPassword(string userName, string password, bool isValid)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest { UserName = userName, Password = password });

        Assert.Equal(isValid, result.IsValid);
    }

    private async Task<ApplicationUser> AddUserAsync(
        string userName, UserRole role = UserRole.Customer, bool isActive = true, int tokenVersion = 0)
    {
        var salt = _crypto.GenerateSalt();
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@majstorio.test",
            PasswordSalt = salt,
            PasswordHash = _crypto.GenerateHash(Password, salt),
            Role = role,
            IsActive = isActive,
            TokenVersion = tokenVersion
        };
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();
        return user;
    }

    public void Dispose() => _ctx.Dispose();
}
