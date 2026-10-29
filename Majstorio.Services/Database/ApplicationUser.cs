using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// A regular user account. Id and CreatedAtUtc are inherited from BaseEntity.
public class ApplicationUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public string? PhoneNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Customer;
    public int TokenVersion { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }
}
