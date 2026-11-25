using System.Security.Cryptography;
using System.Text;
using Majstorio.Common.Services;
using Majstorio.Model.Requests.Auth;
using Majstorio.Model.Responses.Auth;
using Majstorio.Model.Results;
using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class AuthService : IAuthService
{
   
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private const string InvalidResetTokenMessage = "The reset token is invalid or has expired.";
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly MajstorioDbContext _ctx;
    private readonly ICryptoService _cryptoService;
    private readonly ITokenService _tokenService;

    public AuthService(MajstorioDbContext ctx, ICryptoService cryptoService, ITokenService tokenService)
    {
        _ctx = ctx;
        _cryptoService = cryptoService;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _ctx.ApplicationUsers.AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserName == request.UserName);
        if (user == null || !_cryptoService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return Result<LoginResponse>.Failure(ErrorType.Unauthorized, InvalidCredentialsMessage);

        // Checked only after the password, so the response never reveals whether an account exists.
        if (!user.IsActive)
            return Result<LoginResponse>.Failure(ErrorType.Forbidden, "This account has been deactivated. Contact the administrator.");

        var token = _tokenService.CreateAccessToken(user);
        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Role = RoleNames.For(user.Role)
        });
    }

    public async Task<Result> LogoutAsync(int userId)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == userId);
        if (user == null)
            return Result.Failure(ErrorType.NotFound, "User not found.");

        user.TokenVersion++;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == userId);
        if (user == null)
            return Result.Failure(ErrorType.NotFound, "User not found.");

        if (!_cryptoService.VerifyPassword(request.OldPassword, user.PasswordHash, user.PasswordSalt))
            return Result.Failure(ErrorType.Validation, "The old password is not correct.");

        SetPassword(user, request.NewPassword);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<string?>> CreatePasswordResetTokenAsync(ForgotPasswordRequest request)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Email == request.Email && user.IsActive);
        if (user == null)
            return Result<string?>.Success(null);

        // Only a hash is stored, so a copy of the database cannot be used to reset passwords.
        // A new request replaces the previous token.
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = HashResetToken(token);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.Add(ResetTokenLifetime);
        await _ctx.SaveChangesAsync();
        return Result<string?>.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // One message for every failure, so the response does not reveal which accounts exist.
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Email == request.Email && user.IsActive);
        if (user?.PasswordResetTokenHash == null
            || user.PasswordResetTokenExpiresAtUtc == null
            || user.PasswordResetTokenExpiresAtUtc <= DateTime.UtcNow
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.PasswordResetTokenHash),
                Encoding.UTF8.GetBytes(HashResetToken(request.Token))))
            return Result.Failure(ErrorType.Validation, InvalidResetTokenMessage);

        SetPassword(user, request.NewPassword);
        // One-time: the token is cleared together with the password change.
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public Task<bool> IsTokenVersionCurrentAsync(int userId, int tokenVersion) =>
        _ctx.ApplicationUsers.AnyAsync(user => user.Id == userId && user.IsActive && user.TokenVersion == tokenVersion);

    // Stores a new salted hash and invalidates every token issued before the change.
    private void SetPassword(ApplicationUser user, string password)
    {
        user.PasswordSalt = _cryptoService.GenerateSalt();
        user.PasswordHash = _cryptoService.GenerateHash(password, user.PasswordSalt);
        user.TokenVersion++;
    }

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
