using System.Globalization;
using System.Security.Claims;
using Majstorio.Services.Auth;
using Majstorio.Services.Enums;

namespace Majstorio.WebAPI.Services.Auth;

public static class ClaimsPrincipalExtensions
{
    // Reads the id and the role from the validated JWT. Controllers pass this to the services,
    // so who the caller is never comes from the request body or the URL.
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal) => new(
        int.Parse(principal.FindFirstValue(JwtClaimNames.UserId)!, CultureInfo.InvariantCulture),
        Enum.Parse<UserRole>(principal.FindFirstValue(JwtClaimNames.Role)!));
}
