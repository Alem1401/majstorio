namespace Majstorio.Model.Responses.Notification;

public class NotificationResponse
{
    public int Id { get; set; }
    public int? ServiceRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
