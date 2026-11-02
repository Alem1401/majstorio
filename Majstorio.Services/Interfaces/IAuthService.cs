using Majstorio.Model.Requests.Auth;
using Majstorio.Model.Responses.Auth;
using Majstorio.Model.Results;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

    // Invalidates every token issued to the user so far.
    Task<Result> LogoutAsync(int userId);

    // Used by token validation: false once the user logged out or was deactivated.
    Task<bool> IsTokenVersionCurrentAsync(int userId, int tokenVersion);
}
