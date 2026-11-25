using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Majstorio.WebAPI.Hubs;


[Authorize]
public class NotificationHub : Hub
{
}
