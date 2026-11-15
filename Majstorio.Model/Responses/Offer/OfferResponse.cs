namespace Majstorio.Model.Responses.Offer;

public class OfferResponse
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int TradespersonProfileId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string TradespersonName { get; set; } = string.Empty;
    public string ServiceCategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
    // 0 = Pending, 1 = Accepted, 2 = Rejected, 3 = Withdrawn, 4 = Closed.
    public int Status { get; set; }
    public string? DecisionReason { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}
