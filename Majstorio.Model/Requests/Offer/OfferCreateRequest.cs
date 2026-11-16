namespace Majstorio.Model.Requests.Offer;

// The tradesperson is not part of the request: the server uses the profile of the signed-in user.
public class OfferCreateRequest
{
    public int ServiceRequestId { get; set; }
    // In EUR.
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
}
