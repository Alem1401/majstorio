using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;


public class Offer : BaseEntity
{

    public int ServiceRequestId { get; set; }
  
    public int TradespersonProfileId { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Pending;
  
    public string? DecisionReason { get; set; }

    public int? DecidedByUserId { get; set; }
 
    public DateTime? DecidedAtUtc { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public TradespersonProfile TradespersonProfile { get; set; } = null!;
    public ApplicationUser? DecidedByUser { get; set; }
}
