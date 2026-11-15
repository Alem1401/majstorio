using Majstorio.Services.Database;

namespace Majstorio.Services.Extensions;

public static class PersonNames
{
    // "First Last" for list and detail responses; empty when the user was not loaded.
    public static string Full(ApplicationUser? user)
        => user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
}
