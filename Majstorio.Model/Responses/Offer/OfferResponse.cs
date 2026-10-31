namespace Majstorio.Model.Responses.Offer;

public class OfferResponse
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int TradespersonProfileId { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
   
    public int Status { get; set; }
    public string? DecisionReason { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}
