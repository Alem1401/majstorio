using System.Text.Json;
using Majstorio.Common.Services;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// Registration, password change and password reset.
public class AccountTests : IDisposable
{
    private const string Password = "Correct-Horse-1";
    private const string NewPassword = "Battery-Staple-2";

    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly CryptoService _crypto = new();
    private readonly UserService _users;
    private readonly AuthService _auth;

    public AccountTests()
    {
        _users = new UserService(_ctx, TestSupport.NewMapper(), _crypto);
        _auth = new AuthService(_ctx, _crypto, new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "Majstorio", Audience = "Majstorio.Clients",
            Key = "test-signing-key-that-is-long-enough-for-hs256", ExpiryMinutes = 60
        }), TimeProvider.System));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Register_AlwaysCreatesACustomer_EvenWhenTheBodyAsksForAnotherRole()
    {
        // The body is read the same way the API reads it; the request type has no role to bind to.
        var request = JsonSerializer.Deserialize<UserCreateRequest>(
            """
            {"userName":"ana","email":"ana@majstorio.test","password":"Correct-Horse-1",
             "firstName":"Ana","lastName":"Anic","role":"Administrator","Role":2,"isActive":false}
            """, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var result = await _users.CreateUserAsync(request);

        Assert.True(result.IsSuccess);
        var user = await _ctx.ApplicationUsers.SingleAsync();
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.True(user.IsActive);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.True(_crypto.VerifyPassword(Password, user.PasswordHash, user.PasswordSalt));
        Assert.DoesNotContain("Role", typeof(UserCreateRequest).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task Register_ExistingEmailOrUserName_ReturnsConflict()
    {
        await RegisterAsync("ana");

        var sameEmail = await _users.CreateUserAsync(NewRegistration("other", "ana@majstorio.test"));
        var sameUserName = await _users.CreateUserAsync(NewRegistration("ana", "other@majstorio.test"));

        Assert.Equal(ErrorType.Conflict, sameEmail.ErrorType);
        Assert.Equal(ErrorType.Conflict, sameUserName.ErrorType);
        Assert.Single(await _ctx.ApplicationUsers.ToListAsync());
    }

    [Fact]
    public async Task ChangePassword_WithTheWrongOldPassword_IsRejectedAndKeepsThePassword()
    {
        var user = await RegisterAsync("ana");

        var result = await _auth.ChangePasswordAsync(user.Id, new() { OldPassword = "Wrong-Password-1", NewPassword = NewPassword });

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.True((await _auth.LoginAsync(new() { UserName = "ana", Password = Password })).IsSuccess);
        Assert.True(await _auth.IsTokenVersionCurrentAsync(user.Id, 0));
    }

    [Fact]
    public async Task ChangePassword_WithTheOldPassword_ChangesItAndInvalidatesIssuedTokens()
    {
        var user = await RegisterAsync("ana");

        var result = await _auth.ChangePasswordAsync(user.Id, new() { OldPassword = Password, NewPassword = NewPassword });

        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, (await _auth.LoginAsync(new() { UserName = "ana", Password = Password })).ErrorType);
        Assert.True((await _auth.LoginAsync(new() { UserName = "ana", Password = NewPassword })).IsSuccess);
        Assert.False(await _auth.IsTokenVersionCurrentAsync(user.Id, 0));
    }

    [Fact]
    public async Task ResetPassword_WithTheIssuedToken_WorksExactlyOnce()
    {
        var user = await RegisterAsync("ana");
        var token = (await _auth.CreatePasswordResetTokenAsync(new() { Email = user.Email })).Value!;
        // Only a hash of the token is stored, with an expiry a few minutes ahead.
        Assert.NotEqual(token, user.PasswordResetTokenHash);
        Assert.InRange(user.PasswordResetTokenExpiresAtUtc!.Value, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(16));
        var reset = new Majstorio.Model.Requests.Auth.ResetPasswordRequest { Email = user.Email, Token = token, NewPassword = NewPassword };

        Assert.True((await _auth.ResetPasswordAsync(reset)).IsSuccess);

        Assert.True((await _auth.LoginAsync(new() { UserName = "ana", Password = NewPassword })).IsSuccess);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.False(await _auth.IsTokenVersionCurrentAsync(user.Id, 0));
        reset.NewPassword = "Third-Password-3";
        Assert.Equal(ErrorType.Validation, (await _auth.ResetPasswordAsync(reset)).ErrorType);
        Assert.True((await _auth.LoginAsync(new() { UserName = "ana", Password = NewPassword })).IsSuccess);
    }

    [Fact]
    public async Task ResetPassword_WithAWrongOrExpiredToken_IsRejected()
    {
        var user = await RegisterAsync("ana");
        var token = (await _auth.CreatePasswordResetTokenAsync(new() { Email = user.Email })).Value!;

        var wrong = await _auth.ResetPasswordAsync(new() { Email = user.Email, Token = "not-the-token", NewPassword = NewPassword });
        var wrongAccount = await _auth.ResetPasswordAsync(new() { Email = "nobody@majstorio.test", Token = token, NewPassword = NewPassword });
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await _ctx.SaveChangesAsync();
        var expired = await _auth.ResetPasswordAsync(new() { Email = user.Email, Token = token, NewPassword = NewPassword });

        Assert.Equal(ErrorType.Validation, wrong.ErrorType);
        Assert.Equal(ErrorType.Validation, wrongAccount.ErrorType);
        Assert.Equal(ErrorType.Validation, expired.ErrorType);
        Assert.Equal(wrong.Message, expired.Message);
        Assert.True((await _auth.LoginAsync(new() { UserName = "ana", Password = Password })).IsSuccess);
    }

    [Fact]
    public async Task ForgotPassword_NewRequestReplacesThePreviousToken()
    {
        var user = await RegisterAsync("ana");
        var first = (await _auth.CreatePasswordResetTokenAsync(new() { Email = user.Email })).Value!;
        var second = (await _auth.CreatePasswordResetTokenAsync(new() { Email = user.Email })).Value!;

        Assert.NotEqual(first, second);
        Assert.Equal(ErrorType.Validation,
            (await _auth.ResetPasswordAsync(new() { Email = user.Email, Token = first, NewPassword = NewPassword })).ErrorType);
        Assert.True((await _auth.ResetPasswordAsync(new() { Email = user.Email, Token = second, NewPassword = NewPassword })).IsSuccess);
    }

    [Fact]
    public async Task ForgotPassword_UnknownOrDeactivatedAccount_SucceedsWithoutAToken()
    {
        var user = await RegisterAsync("ana");
        user.IsActive = false;
        await _ctx.SaveChangesAsync();

        var unknown = await _auth.CreatePasswordResetTokenAsync(new() { Email = "nobody@majstorio.test" });
        var deactivated = await _auth.CreatePasswordResetTokenAsync(new() { Email = user.Email });

        Assert.True(unknown.IsSuccess);
        Assert.Null(unknown.Value);
        Assert.True(deactivated.IsSuccess);
        Assert.Null(deactivated.Value);
    }

    private async Task<ApplicationUser> RegisterAsync(string userName)
    {
        Assert.True((await _users.CreateUserAsync(NewRegistration(userName, $"{userName}@majstorio.test"))).IsSuccess);
        return await _ctx.ApplicationUsers.SingleAsync(user => user.UserName == userName);
    }

    private static UserCreateRequest NewRegistration(string userName, string email) => new()
    {
        UserName = userName, Email = email, Password = Password, FirstName = "Ana", LastName = "Anic"
    };
}
