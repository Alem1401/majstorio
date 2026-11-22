namespace Majstorio.Model.Requests.Review;

public class ReviewSearchRequest : BaseSearchRequest
{
    // Matches reviews with exactly this rating (1 to 5).
    public int? Rating { get; set; }
    // Matches the review of this appointment; a customer uses it to see whether a finished job is rated.
    public int? AppointmentId { get; set; }
}
