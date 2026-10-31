namespace Majstorio.Model.Requests.Offer;

public class OfferCreateRequest
{
    public int ServiceRequestId { get; set; }
    public int TradespersonProfileId { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
}
