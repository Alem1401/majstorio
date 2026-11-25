using System.Security.Claims;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.SignalR;

namespace Majstorio.WebAPI.Hubs;

// Tells SignalR which user a connection belongs to: our token keeps the user id in its own claim.
public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirstValue(JwtClaimNames.UserId);
}
