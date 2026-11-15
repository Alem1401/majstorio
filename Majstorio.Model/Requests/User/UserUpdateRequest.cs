namespace Majstorio.Model.Requests.User;

public class UserUpdateRequest
{
    // Set from the route (PUT /Users/{id}); a value in the body is overwritten.
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; }
    // Customer or Administrator; left out (null) keeps the current role.
    public string? Role { get; set; }
}
