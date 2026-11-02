namespace Majstorio.Model.Requests.Review;

public class ReviewCreateRequest
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
