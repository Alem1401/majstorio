using Majstorio.Model.Requests.Auth;
using Majstorio.Model.Responses.Auth;
using Majstorio.Model.Results;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

    // Invalidates every token issued to the user so far.
    Task<Result> LogoutAsync(int userId);

    // Requires the current password; afterwards every token issued so far stops working.
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    // Issues a one-time reset token that expires shortly. The value is null when no active account has
    // that e-mail; the caller must answer the same way in both cases.
    Task<Result<string?>> CreatePasswordResetTokenAsync(ForgotPasswordRequest request);

    // Sets a new password when the token matches and has not expired; the token cannot be used again.
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);

    // Used by token validation: false once the user logged out or was deactivated.
    Task<bool> IsTokenVersionCurrentAsync(int userId, int tokenVersion);
}
