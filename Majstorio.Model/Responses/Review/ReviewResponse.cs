namespace Majstorio.Model.Responses.Review;

public class ReviewResponse
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int TradespersonProfileId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string TradespersonName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
