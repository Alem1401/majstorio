using Majstorio.Services.Enums;

namespace Majstorio.Services.Auth;

// The signed-in user, read from the JWT by the controller. Services use it for ownership checks,
// so a user id is never taken from a request body.
public sealed record CurrentUser(int Id, UserRole Role)
{
    public bool IsAdministrator => Role == UserRole.Administrator;
}
