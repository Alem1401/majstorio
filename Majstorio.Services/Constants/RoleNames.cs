using Majstorio.Services.Enums;

namespace Majstorio.Services.Constants;

// The single source for role names used in tokens, [Authorize(Roles = ...)] attributes and seed data.
public static class RoleNames
{
    public const string Customer = nameof(UserRole.Customer);
    public const string Tradesperson = nameof(UserRole.Tradesperson);
    public const string Administrator = nameof(UserRole.Administrator);

    public static string For(UserRole role) => role switch
    {
        UserRole.Customer => Customer,
        UserRole.Tradesperson => Tradesperson,
        UserRole.Administrator => Administrator,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.")
    };
}
