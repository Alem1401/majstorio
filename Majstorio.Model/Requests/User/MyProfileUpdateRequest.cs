namespace Majstorio.Model.Requests.User;

// What a signed-in user may change about themselves (PUT /Account).
// The user name, role, active flag and photo are not part of it; the photo has its own upload endpoint.
public class MyProfileUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
