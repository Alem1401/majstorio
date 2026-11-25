using Majstorio.Model.Requests.Notification;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Every signed-in user sees only their own notifications; who that is comes from the token.
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public NotificationsController(INotificationService notificationService, ResultToStatusCodeMapper resultMapper)
    {
        _notificationService = notificationService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] NotificationSearchRequest search)
        => _resultMapper.Map(await _notificationService.GetNotificationsAsync(User.ToCurrentUser(), search));

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
        => _resultMapper.Map(await _notificationService.MarkAsReadAsync(User.ToCurrentUser(), id));

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
        => _resultMapper.Map(await _notificationService.MarkAllAsReadAsync(User.ToCurrentUser()));
}
