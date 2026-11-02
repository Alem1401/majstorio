namespace Majstorio.Services.Database;


public class Notification : BaseEntity
{

    public int UserId { get; set; }

    // Identifier of the RabbitMQ event that produced this row; unique, so a redelivered
    // message cannot create a duplicate notification.
    public Guid EventId { get; set; }

    public int? ServiceRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ServiceRequest? ServiceRequest { get; set; }
}
