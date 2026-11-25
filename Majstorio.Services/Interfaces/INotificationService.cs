using Majstorio.Model.Requests.Notification;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Notification;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface INotificationService
{
    Task<Result<PagedResponse<NotificationResponse>>> GetNotificationsAsync(CurrentUser user, NotificationSearchRequest search);

    Task<Result> MarkAsReadAsync(CurrentUser user, int id);

    Task<Result> MarkAllAsReadAsync(CurrentUser user);
}
