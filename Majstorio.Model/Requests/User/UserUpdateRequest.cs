using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.User;

public class UserUpdateRequest
{
    // Filled in by the controller from the route (PUT /Users/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
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
