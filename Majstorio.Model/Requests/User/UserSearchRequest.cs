namespace Majstorio.Model.Requests.User;

public class UserSearchRequest : BaseSearchRequest
{
    // Matches users whose user name, first name or last name contains this text.
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    // A role name: Customer, Tradesperson or Administrator.
    public string? Role { get; set; }
}
