namespace Majstorio.Model.Responses.Review;

public class ReviewResponse
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int TradespersonProfileId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
