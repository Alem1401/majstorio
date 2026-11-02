namespace Majstorio.Model.Responses.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    // Lets the client choose its start screen; the server still authorizes from the token.
    public string Role { get; set; } = string.Empty;
}
