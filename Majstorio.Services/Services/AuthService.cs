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
    // One message for an unknown user and a wrong password, so usernames cannot be probed.
    private const string InvalidCredentialsMessage = "Invalid username or password.";

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

    public Task<bool> IsTokenVersionCurrentAsync(int userId, int tokenVersion) =>
        _ctx.ApplicationUsers.AnyAsync(user => user.Id == userId && user.IsActive && user.TokenVersion == tokenVersion);
}
