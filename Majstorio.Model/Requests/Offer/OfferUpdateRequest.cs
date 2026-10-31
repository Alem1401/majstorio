namespace Majstorio.Model.Requests.Offer;

public class OfferUpdateRequest
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int TradespersonProfileId { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
    // 0 = Pending, 1 = Accepted, 2 = Rejected, 3 = Withdrawn, 4 = Closed (set by the service).
    public int Status { get; set; }
    public string? DecisionReason { get; set; }
    // The caller must obtain this ID from the authenticated user when recording a decision.
    public int? DecidedByUserId { get; set; }
}
