namespace Majstorio.Model.Requests.Notification;

public class NotificationSearchRequest : BaseSearchRequest
{
    // false = only unread, true = only read, empty = all.
    public bool? IsRead { get; set; }
}
