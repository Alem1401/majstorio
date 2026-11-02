namespace Majstorio.Model.Requests.Review;

public class ReviewUpdateRequest
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
