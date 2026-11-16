using Majstorio.Model.Requests.Auth;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses.Auth;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ResultToStatusCodeMapper _resultMapper;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IUserService userService,
        ResultToStatusCodeMapper resultMapper, IWebHostEnvironment environment)
    {
        _authService = authService;
        _userService = userService;
        _resultMapper = resultMapper;
        _environment = environment;
    }

    // The request has no role field: every registered account is a Customer.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserCreateRequest request)
        => _resultMapper.Map(await _userService.CreateUserAsync(request));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => _resultMapper.Map(await _authService.LoginAsync(request));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
        => _resultMapper.Map(await _authService.LogoutAsync(User.ToCurrentUser().Id));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        => _resultMapper.Map(await _authService.ChangePasswordAsync(User.ToCurrentUser().Id, request));

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.CreatePasswordResetTokenAsync(request);

        // The answer is the same whether or not the e-mail belongs to an account.
        // The project has no e-mail delivery, so the token is returned only in the Development environment.
        return Ok(new ForgotPasswordResponse
        {
            Message = "If an account with that e-mail exists, a password reset token was issued.",
            ResetToken = _environment.IsDevelopment() ? result.Value : null
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        => _resultMapper.Map(await _authService.ResetPasswordAsync(request));
}
