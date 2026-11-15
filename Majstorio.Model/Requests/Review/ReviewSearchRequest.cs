namespace Majstorio.Model.Requests.Review;

public class ReviewSearchRequest : BaseSearchRequest
{
    // Matches reviews with exactly this rating (1 to 5).
    public int? Rating { get; set; }
}
