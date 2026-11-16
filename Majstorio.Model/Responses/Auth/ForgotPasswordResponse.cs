namespace Majstorio.Model.Responses.Auth;

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    // Filled only in the Development environment, because the project has no e-mail delivery.
    public string? ResetToken { get; set; }
}
