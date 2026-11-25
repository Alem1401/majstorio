using Majstorio.Model.Requests.Notification;

namespace Majstorio.Services.Validators.Notification;

// Only paging needs checking; the base class does that.
public class NotificationSearchRequestValidator : SearchRequestValidator<NotificationSearchRequest>
{
}
